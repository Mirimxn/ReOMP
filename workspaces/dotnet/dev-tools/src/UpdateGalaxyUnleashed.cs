using System.IO;

namespace OMP.LSWTSS;

public static class UpdateGalaxyUnleashed
{
    public static void Execute(string gameDirPath)
    {
        BuildGalaxyUnleashed.Execute();

        var galaxyUnleashedInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "galaxy-unleashed"
        );

        File.Delete(
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "index.html"
            )
        );

        File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "index.html"
            ),
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "index.html"
            ),
            true
        );

        File.Delete(
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "galaxy-unleashed-runtime.dll"
            )
        );

        File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "galaxy-unleashed-runtime.dll"
            ),
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "galaxy-unleashed-runtime.dll"
            ),
            true
        );

        File.Delete(
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "galaxy-unleashed-runtime.pdb"
            )
        );

        File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "galaxy-unleashed-runtime.pdb"
            ),
            Path.Combine(
                galaxyUnleashedInstallDirPath,
                "galaxy-unleashed-runtime.pdb"
            ),
            true
        );
    }
}