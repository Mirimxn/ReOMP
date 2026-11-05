using System.IO;

namespace OMP.LSWTSS;

public static class BuildInputHook1
{
    public static void Execute()
    {
        var inputHook1DistDirPath = GetInputHook1DistDirPath.Execute();

        if (Directory.Exists(inputHook1DistDirPath))
        {
            Directory.Delete(inputHook1DistDirPath, true);
        }

        Directory.CreateDirectory(inputHook1DistDirPath);

        BuildInputHook1DotnetPackage.Execute();

        CopyDirectory.IO.CopyDirectory(
            Path.Combine(
                GetInputHook1DotnetPackageDirPath.Execute(),
                "bin",
                "Release",
                "net8.0"
            ),
            inputHook1DistDirPath
        );

        File.Delete(
            Path.Combine(
                inputHook1DistDirPath,
                "omp-lswtss-input-hook1.deps.json"
            )
        );

        File.Delete(
            Path.Combine(
                inputHook1DistDirPath,
                "omp-lswtss-input-hook1.pdb"
            )
        );

        File.Delete(
            Path.Combine(
                inputHook1DistDirPath,
                "omp-lswtss-input-hook1.runtimeconfig.json"
            )
        );

        PackageMod.Execute("input-hook1", inputHook1DistDirPath);
    }
}