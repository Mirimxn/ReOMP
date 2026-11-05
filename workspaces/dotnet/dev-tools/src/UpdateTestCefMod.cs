using System.IO;

namespace OMP.LSWTSS;

public static class UpdateTestCefMod
{
    public static void Execute(string gameDirPath)
    {
        BuildTestCefMod.Execute();

        var testCefModInstallDirPath = Path.Combine(
            gameDirPath,
            "mods",
            "test-cef-mod"
        );

        System.IO.File.Delete(
            Path.Combine(
                testCefModInstallDirPath,
                "index.html"
            )
        );

        System.IO.File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "index.html"
            ),
            Path.Combine(
                testCefModInstallDirPath,
                "index.html"
            ),
            true
        );

        System.IO.File.Delete(
            Path.Combine(
                testCefModInstallDirPath,
                "test-cef-mod.dll"
            )
        );

        System.IO.File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "test-cef-mod.dll"
            ),
            Path.Combine(
                testCefModInstallDirPath,
                "test-cef-mod.dll"
            ),
            true
        );

        System.IO.File.Delete(
            Path.Combine(
                testCefModInstallDirPath,
                "test-cef-mod.pdb"
            )
        );

        System.IO.File.Copy(
            Path.Combine(
                GetTestCefModDistDirPath.Execute(),
                "test-cef-mod.pdb"
            ),
            Path.Combine(
                testCefModInstallDirPath,
                "test-cef-mod.pdb"
            ),
            true
        );
    }
}