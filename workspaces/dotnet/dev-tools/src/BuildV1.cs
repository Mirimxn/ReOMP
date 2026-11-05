using System.IO;

namespace OMP.LSWTSS;

public static class BuildV1
{
    public static void Execute()
    {
        var v1DistDirPath = GetV1DistDirPath.Execute();

        if (Directory.Exists(v1DistDirPath))
        {
            Directory.Delete(v1DistDirPath, true);
        }

        Directory.CreateDirectory(v1DistDirPath);

        var v1ManagerDotnetPackageDirPath = GetModSourceDirPath.Execute("v1", "v1-manager");

        BuildDotnetPackage.Execute(v1ManagerDotnetPackageDirPath);

        CopyDirectory.IO.CopyDirectory(
            Path.Combine(
                v1ManagerDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0"
            ),
            v1DistDirPath
        );

        File.Delete(
            Path.Combine(
                v1DistDirPath,
                "omp-lswtss-v1-manager.deps.json"
            )
        );

        File.Delete(
            Path.Combine(
                v1DistDirPath,
                "omp-lswtss-v1.pdb"
            )
        );

        File.Delete(
            Path.Combine(
                v1DistDirPath,
                "omp-lswtss-v1-manager.pdb"
            )
        );

        File.Delete(
            Path.Combine(
                v1DistDirPath,
                "omp-lswtss-v1-manager.runtimeconfig.json"
            )
        );

        PackageMod.Execute("v1", v1DistDirPath);
    }
}