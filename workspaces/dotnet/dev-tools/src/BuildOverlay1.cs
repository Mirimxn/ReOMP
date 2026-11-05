using System;
using System.Collections.Generic;
using System.IO;

namespace OMP.LSWTSS;

public static class BuildOverlay1
{
    public static void Execute()
    {
        var overlay1DistDirPath = GetOverlay1DistDirPath.Execute();

        // Do NOT wipe the dist directory. When running via package-release.ps1 it is
        // pre-seeded from the upstream bundle, which provides native CEF files
        // (libcef.dll etc.) that are tested for Wine/Proton compatibility. Wiping and
        // rebuilding from the NuGet restore produces a different libcef.dll that crashes
        // on startup under Wine. Instead we overlay new files on top of the seed and
        // only fall back to NuGet natives when the dist has not been seeded.
        Directory.CreateDirectory(overlay1DistDirPath);

        // Build with --arch x64 to get managed CefSharp assemblies (CefSharp.Core.dll,
        // CefSharp.Core.Runtime.dll, etc.) that are not deployed by a RID-less build.
        BuildOverlay1DotnetPackage.Execute();

        var win64Dir = Path.Combine(
            GetOverlay1DotnetPackageDirPath.Execute(),
            "bin", "Release", "net8.0", "win-x64"
        );

        // Native CEF files whose Wine/Proton compatibility depends on the upstream bundle
        // rather than the NuGet restore. If the dist was pre-seeded, keep those versions;
        // only copy from the NuGet build when the file is absent (local dev with no seed).
        var seededNatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "libcef.dll", "chrome_elf.dll", "libEGL.dll", "libGLESv2.dll",
            "d3dcompiler_47.dll", "dxcompiler.dll", "dxil.dll", "icudtl.dat",
            "chrome_100_percent.pak", "chrome_200_percent.pak", "Ijwhost.dll",
        };

        foreach (var srcFile in Directory.EnumerateFiles(win64Dir))
        {
            var name = Path.GetFileName(srcFile);
            var dst = Path.Combine(overlay1DistDirPath, name);
            if ((seededNatives.Contains(name) || name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                && File.Exists(dst))
                continue;
            File.Copy(srcFile, dst, overwrite: true);
        }

        foreach (var srcSubDir in Directory.EnumerateDirectories(win64Dir))
        {
            var dstSubDir = Path.Combine(overlay1DistDirPath, Path.GetFileName(srcSubDir));
            if (!Directory.Exists(dstSubDir))
                CopyDirectory.IO.CopyDirectory(srcSubDir, dstSubDir);
        }

        // Rebuild without --arch so the managed DLL is Any CPU (the win-x64 build
        // produces a PE format that fails to load under Wine's .NET host).
        BuildDotnetPackage.Execute(GetOverlay1DotnetPackageDirPath.Execute());

        File.Copy(
            Path.Combine(
                GetOverlay1DotnetPackageDirPath.Execute(),
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-overlay1.dll"
            ),
            Path.Combine(overlay1DistDirPath, "omp-lswtss-overlay1.dll"),
            overwrite: true
        );

        File.Delete(
            Path.Combine(
                overlay1DistDirPath,
                "omp-lswtss-overlay1.deps.json"
            )
        );

        File.Delete(
            Path.Combine(
                overlay1DistDirPath,
                "omp-lswtss-overlay1.pdb"
            )
        );

        File.Delete(
            Path.Combine(
                overlay1DistDirPath,
                "omp-lswtss-overlay1.runtimeconfig.json"
            )
        );

        PackageMod.Execute("overlay1", overlay1DistDirPath);
    }
}
