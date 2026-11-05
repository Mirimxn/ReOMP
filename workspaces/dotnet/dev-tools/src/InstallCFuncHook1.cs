using System.IO;

namespace OMP.LSWTSS;

public static class InstallCFuncHook1
{
    public static void Execute(string gameDirPath)
    {
        var cFuncHook1InstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "c-func-hook1"
        );
        
        if (Directory.Exists(cFuncHook1InstallDirPath))
        {
            Directory.Delete(cFuncHook1InstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetCFuncHook1DistDirPath.Execute(),
            cFuncHook1InstallDirPath
        );
    }
}