using System.IO;

namespace OMP.LSWTSS;

public static class InstallInputHook1
{
    public static void Execute(string gameDirPath)
    {
        var inputHook1InstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "input-hook1"
        );
        
        if (Directory.Exists(inputHook1InstallDirPath))
        {
            Directory.Delete(inputHook1InstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetInputHook1DistDirPath.Execute(),
            inputHook1InstallDirPath
        );
    }
}