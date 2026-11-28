using System.IO;

namespace OMP.LSWTSS;

public static class InstallPopulationPalette
{
    public static void Execute(string gameDirPath)
    {
        var installDirPath = Path.Combine(gameDirPath, "mods", "population-palette");
        if (Directory.Exists(installDirPath))
        {
            Directory.Delete(installDirPath, true);
        }
        CopyDirectory.IO.CopyDirectory(BuildPopulationPalette.DistDirPath, installDirPath);
    }
}
