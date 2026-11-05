using System;
using System.IO;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static string GuidHex(NuGuid.NativeData g) =>
        $"{g.V1:x2}{g.V2:x2}{g.V3:x2}{g.V4:x2}{g.V5:x2}{g.V6:x2}{g.V7:x2}{g.V8:x2}{g.V9:x2}{g.V10:x2}{g.V11:x2}{g.V12:x2}{g.V13:x2}{g.V14:x2}{g.V15:x2}{g.V16:x2}";

    static NuGuid.NativeData GuidFromBytes(ReadOnlySpan<byte> b) => new()
    {
        V1 = b[0], V2 = b[1], V3 = b[2], V4 = b[3], V5 = b[4], V6 = b[5], V7 = b[6], V8 = b[7],
        V9 = b[8], V10 = b[9], V11 = b[10], V12 = b[11], V13 = b[12], V14 = b[13], V15 = b[14], V16 = b[15],
    };

    static string DescribeGraphHandle(Collectables.GraphVal.NativeHandle graph)
    {
        var h = graph.Val2;
        return $"\"{h.ResourcePath1}\" id1={GuidHex(h.ResourceGuid1NativeData)} ref1={h.ReferenceType1} | " +
               $"\"{h.ResourcePath2}\" id2={GuidHex(h.ResourceGuid2NativeData)} ref2={h.ReferenceType2}";
    }

    // Resource files start with: u32 size, ".CC4HSERHSER", u32 version, u16 length, path\0, 16-byte file id.
    // Further in the header comes the source asset path (the resource path without "_baked") followed by
    // the 16-byte resource id. That second id is the one roster entries reference.
    static bool TryReadModResourceId(string resourcePath, out NuGuid.NativeData id, out string sourceFile)
    {
        id = default;
        sourceFile = "";
        var canon = GetResourceCanonPath(resourcePath);
        var info = _customResourcesInfo.LastOrDefault(r => GetResourceCanonPath(r.Path) == canon);
        if (info == null || !File.Exists(info.SrcPath)) return false;

        var head = new byte[2048];
        int read;
        using (var f = File.OpenRead(info.SrcPath)) read = f.Read(head, 0, head.Length);
        if (read < 40 || !head.AsSpan(4, 12).SequenceEqual(".CC4HSERHSER"u8)) return false;

        var sourcePath = canon.EndsWith("_baked") ? canon[..^"_baked".Length] : canon;
        var pattern = System.Text.Encoding.ASCII.GetBytes(sourcePath + "\0");
        var lower = head.AsSpan(0, read).ToArray();
        for (var i = 0; i < lower.Length; i++) if (lower[i] >= 'A' && lower[i] <= 'Z') lower[i] += 32;
        var at = lower.AsSpan().IndexOf(pattern);
        if (at < 0 || at + pattern.Length + 16 > read)
        {
            VehicleLog($"    '{resourcePath}': source path '{sourcePath}' not found in {Path.GetFileName(info.SrcPath)} header; resource id not set");
            return false;
        }

        id = GuidFromBytes(head.AsSpan(at + pattern.Length, 16));
        sourceFile = info.SrcPath;
        return true;
    }

    static string DescribeGraphState(Collectables.GraphVal.NativeHandle graph)
    {
        try
        {
            var r = graph.Val2.AsNttResourceHandle();
            return $"fileExists={r.IsFileExists()} loaded={r.IsLoaded()} fixedUp={r.IsLoadedAndFixedUp()}";
        }
        catch (Exception e)
        {
            return $"state unknown ({e.Message})";
        }
    }

    // Points a graph column at a resource. The game looks scene graphs up by their resource id, so a
    // path alone is never loaded: for a file shipped by a mod, the id is read from the file itself
    // (the id after its source path) and written to the handle together with the path.
    // A path ending in "@template-id" keeps the resource id copied from the template row.
    const string KeepTemplateIdSuffix = "@template-id";

    static void SetGraphPath(Collectables.GraphVal.NativeHandle graph, string path)
    {
        var h = graph.Val2;
        var keepTemplateId = path.EndsWith(KeepTemplateIdSuffix, StringComparison.OrdinalIgnoreCase);
        if (keepTemplateId) path = path[..^KeepTemplateIdSuffix.Length];
        // Setting a path clears the handle's id, so save the template's ids first
        var templateId1 = h.ResourceGuid1NativeData;
        var templateId2 = h.ResourceGuid2NativeData;
        h.ResourcePath1 = path;
        h.ResourcePath2 = path;
        if (keepTemplateId)
        {
            h.ResourceGuid1NativeData = templateId1;
            h.ResourceGuid2NativeData = templateId2;
            VehicleLog($"    graph '{path}': keeping template resource id {GuidHex(h.ResourceGuid1NativeData)}");
        }
        else if (TryReadModResourceId(path, out var id, out var sourceFile))
        {
            h.ResourceGuid1NativeData = id;
            h.ResourceGuid2NativeData = id;
            VehicleLog($"    graph '{path}': resource id {GuidHex(id)} (from {Path.GetFileName(sourceFile)})");
        }
        else
        {
            h.ResourceGuid1NativeData = default;
            h.ResourceGuid2NativeData = default;
            VehicleLog($"    graph '{path}': not a mod file with a readable id; only the path is set");
        }
        VehicleDebug($"    graph '{path}': {DescribeGraphHandle(graph)} {DescribeGraphState(graph)}");
    }
}
