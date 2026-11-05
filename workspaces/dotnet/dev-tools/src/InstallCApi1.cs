using System.IO;

namespace OMP.LSWTSS;

public static class InstallCApi1
{
    public static void Execute(string gameDirPath)
    {
        var cApi1InstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "c-api1"
        );
        
        if (Directory.Exists(cApi1InstallDirPath))
        {
            Directory.Delete(cApi1InstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetCApi1DistDirPath.Execute(),
            cApi1InstallDirPath
        );
    }
}