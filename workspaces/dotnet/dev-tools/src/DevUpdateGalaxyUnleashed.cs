using System.IO;

namespace OMP.LSWTSS;

public static class DevUpdateGalaxyUnleashed
{
    public static void Execute(string gameDirPath)
    {
        var galaxyUnleashedRuntimeDotnetPackageDirPath = GetModSourceDirPath.Execute("galaxy-unleashed", "galaxy-unleashed-runtime");

        BuildDotnetPackage.Execute(galaxyUnleashedRuntimeDotnetPackageDirPath);

        var galaxyUnleashedInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "galaxy-unleashed"
        );

        File.Delete(
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "omp-lswtss-galaxy-unleashed-runtime.dll"
            )
        );

        File.Copy(
            Path.Combine(
                galaxyUnleashedRuntimeDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-galaxy-unleashed-runtime.dll"
            ),
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "omp-lswtss-galaxy-unleashed-runtime.dll"
            ),
            true
        );

        File.Delete(
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "omp-lswtss-galaxy-unleashed-runtime.pdb"
            )
        );

        File.Copy(
            Path.Combine(
                galaxyUnleashedRuntimeDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-galaxy-unleashed-runtime.pdb"
            ),
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "omp-lswtss-galaxy-unleashed-runtime.pdb"
            ),
            true
        );
    }
}