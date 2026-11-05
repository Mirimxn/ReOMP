using System.IO;

namespace OMP.LSWTSS;

public static class BuildCApi1
{
    public static void Execute(string steamGameDirPath, string egsGameDirPath)
    {
        var cApi1DistDirPath = GetCApi1DistDirPath.Execute();

        if (Directory.Exists(cApi1DistDirPath))
        {
            Directory.Delete(cApi1DistDirPath, true);
        }

        Directory.CreateDirectory(cApi1DistDirPath);

        var cApi1ManagerDotnetPackageDirPath = GetModSourceDirPath.Execute("c-api1", "c-api1-manager");

        BuildCApi1MainDotnetPackage.Execute(steamGameDirPath, egsGameDirPath);

        BuildDotnetPackage.Execute(cApi1ManagerDotnetPackageDirPath);

        File.Copy(
            Path.Combine(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-c-api1-core.dll"
            ),
            Path.Combine(
                cApi1DistDirPath,
                "omp-lswtss-c-api1-core.dll"
            )
        );

        File.Copy(
            Path.Combine(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-c-api1-main.dll"
            ),
            Path.Combine(
                cApi1DistDirPath,
                "omp-lswtss-c-api1-main.dll"
            )
        );

        File.Copy(
            Path.Combine(
                cApi1ManagerDotnetPackageDirPath,
                "bin",
                "Release",
                "net8.0",
                "omp-lswtss-c-api1-manager.dll"
            ),
            Path.Combine(
                cApi1DistDirPath,
                "omp-lswtss-c-api1-manager.dll"
            )
        );

        PackageMod.Execute("c-api1", cApi1DistDirPath);
    }
}