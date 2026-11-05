using System.IO;

namespace OMP.LSWTSS;

public static class BuildTestCefMod
{
    public static void Execute()
    {
        var testCefModDistDirPath = GetTestCefModDistDirPath.Execute();

        if (Directory.Exists(testCefModDistDirPath))
        {
            Directory.Delete(testCefModDistDirPath, true);
        }

        Directory.CreateDirectory(testCefModDistDirPath);

        var testCefModDotnetPackageDirPath = GetModSourceDirPath.Execute("test-cef-mod", "test-cef-mod");

        var testCefModOverlayJsPackageDirPath = GetModSourceDirPath.Execute("test-cef-mod", "test-cef-mod-overlay");

        BuildDotnetPackage.Execute(testCefModDotnetPackageDirPath);
        BuildJsPackage.Execute(testCefModOverlayJsPackageDirPath);

        CopyDirectory.IO.CopyDirectory(
            Path.Combine(
                testCefModDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0"
            ),
            testCefModDistDirPath
        );

        File.Copy(
            Path.Combine(
                testCefModOverlayJsPackageDirPath,
                "dist",
                "index.html"
            ),
            Path.Combine(
                testCefModDistDirPath,
                "index.html"
            )
        );

        PackageMod.Execute("test-cef-mod", testCefModDistDirPath);
    }
}