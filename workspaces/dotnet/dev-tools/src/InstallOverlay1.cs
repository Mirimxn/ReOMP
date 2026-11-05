using System.IO;

namespace OMP.LSWTSS;

public static class InstallOverlay1
{
    public static void Execute(string gameDirPath)
    {
        var overlay1InstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "overlay1"
        );
        
        if (Directory.Exists(overlay1InstallDirPath))
        {
            Directory.Delete(overlay1InstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetOverlay1DistDirPath.Execute(),
            overlay1InstallDirPath
        );
    }
}