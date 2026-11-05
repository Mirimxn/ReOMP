using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Custom roster icons (vehicles and characters).
    //
    // Every entry registered with an "icon" PNG gets a sprite named OMP_<id>.png in the icon sheet
    // of its roster. The first time the game opens that sheet, all its icons are merged into a copy
    // of the original sheet and the game is redirected to the copy. Icons only go into empty cells;
    // when there are none left the sheet grows by whole rows at the bottom, so no existing icon is
    // ever moved or covered. The original sheets are never shipped by mods. They are read from
    //   <game>\omp-data\vehicle-icons\VEHICLES.TSH + VEHICLES_DX11.TEXTURE
    //   <game>\omp-data\character-icons\CHARACTERS.TSH + CHARACTERS_DX11.TEXTURE
    // If they are missing there but an untouched extracted copy sits in the game's UI\ICONS folder,
    // that copy is saved there first. Merged files are cached in each folder's generated\.

    sealed class IconSheet
    {
        public required string Label;      // "Vehicle" / "Character"
        public required string Folder;     // game folder of the sheet, e.g. "UI/ICONS/VEHICLES"
        public required string BaseName;   // "VEHICLES"
        public required string DataDirName;
        public required int Cell;
        public readonly List<CustomIcon> Icons = [];
        public bool Handled;

        public string IndexPath => $"{Folder}/{BaseName}.TSH";
        public string TexturePath => $"{Folder}/{BaseName}_DX11.TEXTURE";
        public string TshName => $"{BaseName}.TSH";
        public string TextureName => $"{BaseName}_DX11.TEXTURE";
        public string DataDir => Path.Combine(Environment.CurrentDirectory, "omp-data", DataDirName);
    }

    sealed record CustomIcon(string EntryId, string SpriteName, string PngPath);

    static readonly IconSheet _vehicleIconSheet = new() { Label = "Vehicle", Folder = "UI/ICONS/VEHICLES", BaseName = "VEHICLES", DataDirName = "vehicle-icons", Cell = 256 };

    static readonly IconSheet _characterIconSheet = new() { Label = "Character", Folder = "UI/ICONS/CHARACTERS", BaseName = "CHARACTERS", DataDirName = "character-icons", Cell = 128 };

    static IconSheet GetIconSheet(string tableName) =>
        string.Equals(tableName, "Characters", StringComparison.OrdinalIgnoreCase) ? _characterIconSheet : _vehicleIconSheet;

    static string GetCustomVehicleIconSpriteName(string entryId) => $"OMP_{entryId}.png";

    static void RegisterCustomVehicleIcon(string tableName, string entryId, string pngPath)
    {
        var sheet = GetIconSheet(tableName);
        sheet.Icons.RemoveAll(i => i.EntryId == entryId);
        sheet.Icons.Add(new CustomIcon(entryId, GetCustomVehicleIconSpriteName(entryId), pngPath));
    }

    // Called from the file hooks for every file the game opens from its DAT archives.
    static void EnsureVehicleIconSheet(string requestedPath)
    {
        if (_vehicleDebugLog &&
            (requestedPath.Contains("ui/icons", StringComparison.OrdinalIgnoreCase) ||
             requestedPath.Contains("chars/minifig", StringComparison.OrdinalIgnoreCase) ||
             requestedPath.Contains("chars/vehicles", StringComparison.OrdinalIgnoreCase)))
        {
            var served = FetchIsDiskResourceRegistered(GetResourceCanonPath(requestedPath));
            VehicleDebug($"file request: {requestedPath} -> {(served ? "mod file" : "game DAT")}");
        }

        foreach (var sheet in new[] { _vehicleIconSheet, _characterIconSheet })
        {
            if (sheet.Handled || sheet.Icons.Count == 0) continue;

            var canon = GetResourceCanonPath(requestedPath);
            if (!canon.EndsWith(GetResourceCanonPath(sheet.IndexPath)) &&
                !canon.EndsWith(GetResourceCanonPath(sheet.TexturePath)))
            {
                continue;
            }

            sheet.Handled = true;

            try
            {
                BuildAndLinkIconSheet(sheet);
            }
            catch (Exception e)
            {
                VehicleLog($"{sheet.Label} icons: FAILED, using the game's original icons. {e}");
            }
        }
    }

    // Saves an extracted original sheet from the game's UI folder into omp-data, if it is untouched.
    static void TryAdoptLooseOriginal(IconSheet sheet)
    {
        var looseDir = Path.Combine(Environment.CurrentDirectory, sheet.Folder.Replace('/', Path.DirectorySeparatorChar));
        var looseTsh = FindFileIgnoreCase(looseDir, sheet.TshName);
        var looseTexture = FindFileIgnoreCase(looseDir, sheet.TextureName);
        if (looseTsh == null || looseTexture == null) return;

        try
        {
            VehicleIconAtlas.Cell = sheet.Cell;
            var index = new VehicleIconAtlas.SpriteIndex(File.ReadAllBytes(looseTsh));
            if (index.Sprites.Any(s => s.Name.StartsWith("OMP_", StringComparison.OrdinalIgnoreCase)))
            {
                VehicleLog($"{sheet.Label} icons: the sheet in {looseDir} already has OMP icons, not using it as the original.");
                return;
            }
            Directory.CreateDirectory(sheet.DataDir);
            File.Copy(looseTsh, Path.Combine(sheet.DataDir, sheet.TshName), false);
            File.Copy(looseTexture, Path.Combine(sheet.DataDir, sheet.TextureName), false);
            VehicleLog($"{sheet.Label} icons: saved the original sheet from {looseDir} into {sheet.DataDir}");
        }
        catch (Exception e)
        {
            VehicleLog($"{sheet.Label} icons: could not save the original sheet from {looseDir}: {e.Message}");
        }
    }

    static void BuildAndLinkIconSheet(IconSheet sheet)
    {
        var dataDir = sheet.DataDir;
        var baseTsh = FindFileIgnoreCase(dataDir, sheet.TshName);
        var baseTexture = FindFileIgnoreCase(dataDir, sheet.TextureName);
        if (baseTsh == null || baseTexture == null)
        {
            TryAdoptLooseOriginal(sheet);
            baseTsh = FindFileIgnoreCase(dataDir, sheet.TshName);
            baseTexture = FindFileIgnoreCase(dataDir, sheet.TextureName);
        }
        if (baseTsh == null || baseTexture == null)
        {
            Directory.CreateDirectory(dataDir);
            VehicleLog($"{sheet.Label} icons: skipped. Copy the game's original {sheet.TshName} and {sheet.TextureName} " +
                       $"(from {sheet.Folder.ToLowerInvariant()} in the DAT files) into {dataDir}");
            return;
        }

        var icons = sheet.Icons
            .OrderBy(i => i.EntryId, StringComparer.OrdinalIgnoreCase)
            .Where(i =>
            {
                if (File.Exists(i.PngPath)) return true;
                VehicleLog($"{sheet.Label} icons: '{i.EntryId}': icon not found: {i.PngPath}");
                return false;
            })
            .ToList();
        if (icons.Count == 0) return;

        // Cache key: base files + every icon (name, content)
        var keyBuilder = new StringBuilder($"v2|{sheet.Cell}|");
        foreach (var f in new[] { baseTsh, baseTexture })
        {
            var info = new FileInfo(f);
            keyBuilder.Append($"{info.Length}:{info.LastWriteTimeUtc.Ticks}|");
        }
        foreach (var i in icons)
        {
            keyBuilder.Append(i.SpriteName).Append(':').Append(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(i.PngPath)))).Append('|');
        }
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyBuilder.ToString())));

        var outDir = Path.Combine(dataDir, "generated");
        var outTsh = Path.Combine(outDir, sheet.TshName);
        var outTexture = Path.Combine(outDir, sheet.TextureName);
        var keyFile = Path.Combine(outDir, "cache.key");

        if (File.Exists(outTsh) && File.Exists(outTexture) && File.Exists(keyFile) && File.ReadAllText(keyFile) == key)
        {
            VehicleLog($"{sheet.Label} icons: using cached sheet ({icons.Count} icon(s))");
        }
        else
        {
            var images = new List<(string Name, byte[] Rgba)>();
            foreach (var i in icons)
            {
                try
                {
                    var (w, h, rgba) = PngImage.Load(i.PngPath);
                    images.Add((i.SpriteName, PngImage.FitSquare(w, h, rgba, sheet.Cell)));
                }
                catch (Exception e)
                {
                    VehicleLog($"{sheet.Label} icons: '{i.EntryId}': could not read {i.PngPath}: {e.Message}");
                }
            }
            if (images.Count == 0) return;

            var (tsh, texture, placements, addedRows) = VehicleIconAtlas.Build(File.ReadAllBytes(baseTsh), File.ReadAllBytes(baseTexture), images, sheet.Cell);

            Directory.CreateDirectory(outDir);
            // Write to temp files then move, so a crash never leaves half-written files behind
            File.WriteAllBytes(outTsh + ".tmp", tsh);
            File.WriteAllBytes(outTexture + ".tmp", texture);
            File.Move(outTsh + ".tmp", outTsh, true);
            File.Move(outTexture + ".tmp", outTexture, true);
            File.WriteAllText(keyFile, key);

            foreach (var p in placements)
            {
                VehicleLog($"{sheet.Label} icons: {p.Name} -> cell ({p.X / sheet.Cell},{p.Y / sheet.Cell}){(p.New ? "" : " (replaced existing)")}");
            }
            VehicleLog($"{sheet.Label} icons: built sheet with {placements.Count} custom icon(s){(addedRows > 0 ? $", grew by {addedRows} row(s)" : "")}");
        }

        LinkCustomResource(new CustomResourceInfo { Path = sheet.IndexPath, SrcPath = outTsh });
        LinkCustomResource(new CustomResourceInfo { Path = sheet.TexturePath, SrcPath = outTexture });
    }

    static string? FindFileIgnoreCase(string dir, string fileName)
    {
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir).FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
    }
}
