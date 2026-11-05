using System;
using System.IO;

namespace OMP.LSWTSS;

public static class BuildGalaxyUnleashed
{
    public static void Execute()
    {
        var galaxyUnleashedDistDirPath = GetGalaxyUnleashedDistDirPath.Execute();

        // Do NOT wipe the dist directory. When running via package-release.ps1 it is
        // pre-seeded from the upstream bundle with a working index.html. If the JS
        // toolchain (Node/Yarn) is unavailable the JS build will fail, but the .NET
        // runtime DLL can still be built and must be written to dist. Wiping first
        // would cause package-release.ps1 to restore the entire component from the
        // upstream bundle, which may be older than the current .NET source.
        Directory.CreateDirectory(galaxyUnleashedDistDirPath);

        var galaxyUnleashedRuntimeDotnetPackageDirPath = GetModSourceDirPath.Execute("galaxy-unleashed", "galaxy-unleashed-runtime");

        var galaxyUnleashedOverlayJsPackageDirPath = GetModSourceDirPath.Execute("galaxy-unleashed", "galaxy-unleashed-overlay");

        BuildDotnetPackage.Execute(galaxyUnleashedRuntimeDotnetPackageDirPath);

        // Copy the .NET output first so the DLL is always current regardless of
        // whether the JS build succeeds below. Use per-file copy with overwrite
        // so files seeded from the upstream bundle are replaced with fresh builds.
        var dotnetOutputDir = Path.Combine(
            galaxyUnleashedRuntimeDotnetPackageDirPath,
            "bin", "Release", "net8.0"
        );
        foreach (var srcFile in Directory.EnumerateFiles(dotnetOutputDir))
            File.Copy(srcFile, Path.Combine(galaxyUnleashedDistDirPath, Path.GetFileName(srcFile)), overwrite: true);
        foreach (var srcSubDir in Directory.EnumerateDirectories(dotnetOutputDir))
        {
            var dstSubDir = Path.Combine(galaxyUnleashedDistDirPath, Path.GetFileName(srcSubDir));
            if (!Directory.Exists(dstSubDir))
                CopyDirectory.IO.CopyDirectory(srcSubDir, dstSubDir);
        }

        // Build the overlay JS bundle. If Node/Yarn is not available this throws;
        // the catch below leaves the index.html that was seeded from the upstream
        // bundle so the overlay still has a working (if older) UI.
        var indexHtmlDst = Path.Combine(galaxyUnleashedDistDirPath, "index.html");
        try
        {
            BuildJsPackage.Execute(galaxyUnleashedOverlayJsPackageDirPath);
            File.Copy(
                Path.Combine(galaxyUnleashedOverlayJsPackageDirPath, "dist", "index.html"),
                indexHtmlDst,
                overwrite: true
            );
        }
        catch
        {
            if (!File.Exists(indexHtmlDst))
                throw; // no fallback available; surface the error
            Console.WriteLine("BuildGalaxyUnleashed: JS build failed; keeping existing index.html.");
        }

        PackageMod.Execute("galaxy-unleashed", galaxyUnleashedDistDirPath);
    }
}