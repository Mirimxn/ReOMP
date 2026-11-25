using System.IO;

namespace OMP.LSWTSS;

public static class InstallAssetReplacement
{
    public static void Execute(string gameDirPath)
    {
        var installDirPath = Path.Combine(gameDirPath, "mods", "asset-replacement");
        if (Directory.Exists(installDirPath))
        {
            Directory.Delete(installDirPath, true);
        }
        CopyDirectory.IO.CopyDirectory(BuildAssetReplacement.DistDirPath, installDirPath);
    }
}
