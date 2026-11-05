using System.IO;

namespace OMP.LSWTSS;

public static class InstallGalaxyUnleashed
{
    public static void Execute(string gameDirPath)
    {
        var galaxyUnleashedInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "galaxy-unleashed"
        );
        
        if (Directory.Exists(galaxyUnleashedInstallDirPath))
        {
            Directory.Delete(galaxyUnleashedInstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetGalaxyUnleashedDistDirPath.Execute(),
            galaxyUnleashedInstallDirPath
        );
    }
}