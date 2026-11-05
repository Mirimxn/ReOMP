using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Custom roster previews: see CApi1UIRenderTargetModelLoaderLoadGraphMethodHook.cs.

    static string GetCustomVehiclePreviewPath(string vehicleId) =>
        $"chars/vehicles/omp_previews/{vehicleId.ToLowerInvariant()}_cheap.scene_baked";

    // Gives each custom vehicle with a "previewScene" its own preview path, routed by the LoadGraph hook.
    static void AssignCustomVehiclePreviews(Collectables.Table.NativeHandle table, string tableName)
    {
        var wanted = _customVehiclesInfo
            .Where(v => v.PreviewScenePath != null && !v.DryRun && string.Equals(v.TableName, tableName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (wanted.Count == 0) return;

        int cheapCol = FindColumn(table, "Cheap Graph");
        if (cheapCol < 0) { VehicleLog("Vehicle previews: 'Cheap Graph' column not found"); return; }

        foreach (var info in wanted)
        {
            if (FindEntryIndex(table, info.EntryName) is not uint rowIndex) continue;
            if (!File.Exists(info.PreviewScenePath)) { VehicleLog($"Vehicle previews: '{info.Id}': scene not found: {info.PreviewScenePath}"); continue; }

            var cheap = ((Collectables.GraphVal.NativeHandle)GetValuePtr(table.Values[rowIndex], (uint)cheapCol)).Val2;
            var basePath = cheap.ResourcePath1 ?? "";   // copied from the template row
            var baseId = cheap.ResourceGuid1NativeData;
            var baseName = info.TemplateMatch;
            if (!string.IsNullOrEmpty(info.PreviewBase))
            {
                if (FindEntryIndex(table, info.PreviewBase) is uint b &&
                    GetValuePtr(table.Values[b], (uint)cheapCol) is var bp && bp != nint.Zero &&
                    ((Collectables.GraphVal.NativeHandle)bp).Val2.ResourcePath1 is string p && p.EndsWith(".scene_baked", StringComparison.OrdinalIgnoreCase))
                {
                    basePath = p; baseId = ((Collectables.GraphVal.NativeHandle)bp).Val2.ResourceGuid1NativeData; baseName = info.PreviewBase;
                }
                else VehicleLog($"Vehicle previews: '{info.Id}': previewBase '{info.PreviewBase}' not found or has no preview scene; using the template's");
            }
            if (basePath.Length == 0) { VehicleLog($"Vehicle previews: '{info.Id}': no preview slot to borrow"); continue; }

            // The scene is served at the base's path, so its header must carry that path and id.
            var sourcePath = basePath.ToLowerInvariant();
            if (sourcePath.EndsWith("_baked")) sourcePath = sourcePath[..^"_baked".Length];
            var headerId = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes("omp-vehicle-preview-header:" + info.Id.ToLowerInvariant()));
            headerId[6] = (byte)((headerId[6] & 0x0f) | 0x40);
            headerId[8] = (byte)((headerId[8] & 0x3f) | 0x80);
            var original = File.ReadAllBytes(info.PreviewScenePath!);
            var scene = info.PreviewKeepHeader ? original : PatchSceneHeader(original, basePath, sourcePath, GuidBytes(baseId), headerId);
            var outDir = Path.Combine(Environment.CurrentDirectory, "omp-data", "vehicle-previews");
            Directory.CreateDirectory(outDir);
            var outFile = Path.Combine(outDir, $"{info.Id}.scene_baked");
            File.WriteAllBytes(outFile + ".tmp", scene);
            File.Move(outFile + ".tmp", outFile, true);

            var path = GetCustomVehiclePreviewPath(info.Id);
            var id1 = cheap.ResourceGuid1NativeData;   // setting a path clears the id: keep the template's
            var id2 = cheap.ResourceGuid2NativeData;
            cheap.ResourcePath1 = path;
            cheap.ResourcePath2 = path;
            cheap.ResourceGuid1NativeData = id1;
            cheap.ResourceGuid2NativeData = id2;
            _customPreviewRoutes[path] = new CustomPreviewRoute(info.Id, basePath, outFile);
            var templatePath = $"{basePath} (slot of '{baseName}')";
            VehicleLog($"Vehicle previews: '{info.Id}' -> {path} (shown through {templatePath})");
        }
    }

    static int FindColumn(Collectables.Table.NativeHandle table, string name)
    {
        for (int c = 0; c < _columnKinds.Length; c++)
        {
            string? n = null;
            try { n = table.GetColumnName(c); } catch { }
            if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return c;
        }
        return -1;
    }

    static byte[] GuidBytes(NuGuid.NativeData g) =>
        [g.V1, g.V2, g.V3, g.V4, g.V5, g.V6, g.V7, g.V8, g.V9, g.V10, g.V11, g.V12, g.V13, g.V14, g.V15, g.V16];

    // Scene files start with a header chunk (BE u32 size) holding: magic, version, the scene's own path
    // (BE u16 length incl. NUL, at offset 20), ..., and the source path (BE u16 length, "....scene\0")
    // followed by the 16-byte resource id. Both paths are rewritten and the chunk size updated.
    static byte[] PatchSceneHeader(byte[] d, string selfPath, string sourcePath, byte[] sourceId, byte[] headerId)
    {
        static int Be16(byte[] b, int o) => (b[o] << 8) | b[o + 1];
        static int Be32(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
        static byte[] Str16(string s) { var t = Encoding.ASCII.GetBytes(s); var r = new byte[t.Length + 3]; r[0] = (byte)((t.Length + 1) >> 8); r[1] = (byte)(t.Length + 1); t.CopyTo(r, 2); return r; }

        var headerEnd = 4 + Be32(d, 0);
        if (headerEnd > d.Length || !d.AsSpan(4, 12).SequenceEqual(".CC4HSERHSER"u8)) throw new InvalidDataException("not a scene file");

        var selfLen = Be16(d, 20);
        var body = new List<byte>();
        body.AddRange(d[4..20]);
        body.AddRange(Str16(selfPath));
        // the scene's own resource id follows its path: give every copy its own, or it clashes with the
        // scene it was made from (and with every other copy of it)
        body.AddRange(headerId);
        var rest = d[(22 + selfLen + 16)..headerEnd];

        // find the source path: a ".scene\0" string preceded by its BE u16 length
        var marker = ".scene\0"u8.ToArray();
        int at = -1;
        for (int i = 0; i + marker.Length <= rest.Length; i++)
        {
            if (!rest.AsSpan(i, marker.Length).SequenceEqual(marker)) continue;
            int end = i + marker.Length; // just past NUL
            for (int s = i; s >= 2; s--)
            {
                if (Be16(rest, s - 2) == end - s && rest[s] >= 0x20) { at = s - 2; break; }
                if (rest[s - 1] < 0x20) break;
            }
            if (at >= 0)
            {
                var oldLen = Be16(rest, at);
                body.AddRange(rest[..at]);
                body.AddRange(Str16(sourcePath));
                body.AddRange(sourceId);
                body.AddRange(rest[(at + 2 + oldLen + 16)..]);
                break;
            }
        }
        if (at < 0) throw new InvalidDataException("source path not found in scene header");

        var outBytes = new List<byte>(d.Length + 64);
        var size = body.Count;
        outBytes.AddRange(new byte[] { (byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size });
        outBytes.AddRange(body);
        outBytes.AddRange(d[headerEnd..]);
        return outBytes.ToArray();
    }
}
