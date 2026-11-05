using System.IO;

namespace OMP.LSWTSS;

public static class BuildDebugTools
{
    public static void Execute()
    {
        var debugToolsDistDirPath = GetDebugToolsDistDirPath.Execute();

        if (Directory.Exists(debugToolsDistDirPath))
        {
            Directory.Delete(debugToolsDistDirPath, true);
        }

        Directory.CreateDirectory(debugToolsDistDirPath);

        var debugToolsDotnetPackageDirPath = GetModSourceDirPath.Execute("debug-tools", "debug-tools");

        BuildDotnetPackage.Execute(debugToolsDotnetPackageDirPath);

        CopyDirectory.IO.CopyDirectory(
            Path.Combine(
                debugToolsDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0"
            ),
            debugToolsDistDirPath
        );

        File.Delete(
            Path.Combine(
                debugToolsDistDirPath,
                "omp-lswtss-debug-tools.deps.json"
            )
        );

        File.Delete(
            Path.Combine(
                debugToolsDistDirPath,
                "omp-lswtss-debug-tools.pdb"
            )
        );

        File.Delete(
            Path.Combine(
                debugToolsDistDirPath,
                "omp-lswtss-debug-tools.runtimeconfig.json"
            )
        );

        PackageMod.Execute("debug-tools", debugToolsDistDirPath);
    }
}