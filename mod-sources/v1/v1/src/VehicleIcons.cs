using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Custom vehicle icons.
    //
    // Every vehicle registered with an "icon" PNG gets a sprite named OMP_<id>.png.
    // The first time the game opens the vehicle icon sheet, all icons are merged into a copy of
    // the original sheet (free cells first, extra rows when it is full) and the game is redirected
    // to that copy. The originals are never shipped by mods: each player extracts them once into
    //   <game>\omp-data\vehicle-icons\VEHICLES.TSH
    //   <game>\omp-data\vehicle-icons\VEHICLES_DX11.TEXTURE
    // The merged files are cached in <game>\omp-data\vehicle-icons\generated\.

    const string VehicleIconSheetIndexPath = "UI/ICONS/VEHICLES/VEHICLES.TSH";
    const string VehicleIconSheetTexturePath = "UI/ICONS/VEHICLES/VEHICLES_DX11.TEXTURE";

    sealed record CustomVehicleIcon(string VehicleId, string SpriteName, string PngPath);

    static readonly List<CustomVehicleIcon> _customVehicleIcons = [];

    static bool _vehicleIconSheetHandled = false;

    static string VehicleIconsDataDir => Path.Combine(Environment.CurrentDirectory, "omp-data", "vehicle-icons");

    static string GetCustomVehicleIconSpriteName(string vehicleId) => $"OMP_{vehicleId}.png";

    static void RegisterCustomVehicleIcon(string vehicleId, string pngPath)
    {
        _customVehicleIcons.RemoveAll(i => i.VehicleId == vehicleId);
        _customVehicleIcons.Add(new CustomVehicleIcon(vehicleId, GetCustomVehicleIconSpriteName(vehicleId), pngPath));
    }

    // Called from the file hooks for every file the game opens from its DAT archives.
    static void EnsureVehicleIconSheet(string requestedPath)
    {
        if (_vehicleDebugLog &&
            (requestedPath.Contains("icons/vehicles", StringComparison.OrdinalIgnoreCase) ||
             requestedPath.Contains("chars/vehicles", StringComparison.OrdinalIgnoreCase)))
        {
            var served = FetchIsDiskResourceRegistered(GetResourceCanonPath(requestedPath));
            VehicleDebug($"file request: {requestedPath} -> {(served ? "mod file" : "game DAT")}");
        }

        if (_vehicleIconSheetHandled || _customVehicleIcons.Count == 0) return;

        var canon = GetResourceCanonPath(requestedPath);
        if (!canon.EndsWith(GetResourceCanonPath(VehicleIconSheetIndexPath)) &&
            !canon.EndsWith(GetResourceCanonPath(VehicleIconSheetTexturePath)))
        {
            return;
        }

        _vehicleIconSheetHandled = true;

        try
        {
            BuildAndLinkVehicleIconSheet();
        }
        catch (Exception e)
        {
            VehicleLog($"Vehicle icons: FAILED, using the game's original icons. {e}");
        }
    }

    static void BuildAndLinkVehicleIconSheet()
    {
        var dataDir = VehicleIconsDataDir;
        var baseTsh = FindFileIgnoreCase(dataDir, "VEHICLES.TSH");
        var baseTexture = FindFileIgnoreCase(dataDir, "VEHICLES_DX11.TEXTURE");
        if (baseTsh == null || baseTexture == null)
        {
            Directory.CreateDirectory(dataDir);
            VehicleLog($"Vehicle icons: skipped. Copy the game's original VEHICLES.TSH and VEHICLES_DX11.TEXTURE " +
                       $"(from ui\\icons\\vehicles in the DAT files) into {dataDir}");
            return;
        }

        var icons = _customVehicleIcons
            .OrderBy(i => i.VehicleId, StringComparer.OrdinalIgnoreCase)
            .Where(i =>
            {
                if (File.Exists(i.PngPath)) return true;
                VehicleLog($"Vehicle icons: '{i.VehicleId}': icon not found: {i.PngPath}");
                return false;
            })
            .ToList();
        if (icons.Count == 0) return;

        // Cache key: base files + every icon (name, content)
        var keyBuilder = new StringBuilder("v1|");
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
        var outTsh = Path.Combine(outDir, "VEHICLES.TSH");
        var outTexture = Path.Combine(outDir, "VEHICLES_DX11.TEXTURE");
        var keyFile = Path.Combine(outDir, "cache.key");

        if (File.Exists(outTsh) && File.Exists(outTexture) && File.Exists(keyFile) && File.ReadAllText(keyFile) == key)
        {
            VehicleLog($"Vehicle icons: using cached sheet ({icons.Count} icon(s))");
        }
        else
        {
            var images = new List<(string Name, byte[] Rgba)>();
            foreach (var i in icons)
            {
                try
                {
                    var (w, h, rgba) = PngImage.Load(i.PngPath);
                    images.Add((i.SpriteName, PngImage.FitSquare(w, h, rgba, VehicleIconAtlas.Cell)));
                }
                catch (Exception e)
                {
                    VehicleLog($"Vehicle icons: '{i.VehicleId}': could not read {i.PngPath}: {e.Message}");
                }
            }
            if (images.Count == 0) return;

            var (tsh, texture, placements, addedRows) = VehicleIconAtlas.Build(File.ReadAllBytes(baseTsh), File.ReadAllBytes(baseTexture), images);

            Directory.CreateDirectory(outDir);
            // Write to temp files then move, so a crash never leaves half-written files behind
            File.WriteAllBytes(outTsh + ".tmp", tsh);
            File.WriteAllBytes(outTexture + ".tmp", texture);
            File.Move(outTsh + ".tmp", outTsh, true);
            File.Move(outTexture + ".tmp", outTexture, true);
            File.WriteAllText(keyFile, key);

            foreach (var p in placements)
            {
                VehicleLog($"Vehicle icons: {p.Name} -> cell ({p.X / VehicleIconAtlas.Cell},{p.Y / VehicleIconAtlas.Cell}){(p.New ? "" : " (replaced existing)")}");
            }
            VehicleLog($"Vehicle icons: built sheet with {placements.Count} custom icon(s){(addedRows > 0 ? $", grew by {addedRows} row(s)" : "")}");
        }

        LinkCustomResource(new CustomResourceInfo { Path = VehicleIconSheetIndexPath, SrcPath = outTsh });
        LinkCustomResource(new CustomResourceInfo { Path = VehicleIconSheetTexturePath, SrcPath = outTexture });
    }

    static string? FindFileIgnoreCase(string dir, string fileName)
    {
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir).FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
    }
}
