using System.IO;

namespace OMP.LSWTSS;

public static class InstallDebugTools
{
    public static void Execute(string gameDirPath)
    {
        var debugToolsInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "debug-tools"
        );
        
        if (Directory.Exists(debugToolsInstallDirPath))
        {
            Directory.Delete(debugToolsInstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetDebugToolsDistDirPath.Execute(),
            debugToolsInstallDirPath
        );
    }
}