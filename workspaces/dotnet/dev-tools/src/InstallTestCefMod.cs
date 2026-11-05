using System.IO;

namespace OMP.LSWTSS;

public static class InstallTestCefMod
{
    public static void Execute(string gameDirPath)
    {
        var testCefModInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "test-cef-mod"
        );
        
        if (Directory.Exists(testCefModInstallDirPath))
        {
            Directory.Delete(testCefModInstallDirPath, true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetTestCefModDistDirPath.Execute(),
            testCefModInstallDirPath
        );
    }
}