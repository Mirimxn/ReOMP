using System.IO;

namespace OMP.LSWTSS;

public static class InstallV1
{
    public static void Execute(string gameDirPath)
    {
        var v1InstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "v1"
        );
        
        if (Directory.Exists(v1InstallDirPath))
        {
            Directory.Delete(v1InstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetV1DistDirPath.Execute(),
            v1InstallDirPath
        );
    }
}