using System.IO;

namespace OMP.LSWTSS;

public static class RunCApi1Scraper
{
    public static void Execute(string gameDirPath, GameKind gameKind)
    {
        BuildDriverDinput8RustPackage.Execute();
        InstallDriverDinput8Library.Execute(gameDirPath);

        BuildDriverRustPackage.Execute();
        InstallDriverLibrary.Execute(gameDirPath);

        BuildCApi1ScraperEngineDotnetPackage.Execute();

        InstallDriverConfig.Execute(
            gameDirPath,
            new DriverConfig
            {
                EngineAssemblyName = "omp-lswtss-c-api1-scraper-engine",
                EngineAssemblyPath = Path.GetFullPath(
                    Path.Combine(
                        GetCApi1ScraperEnginePackageDirPath.Execute(),
                        "bin",
                        "Release",
                        "net8.0",
                        "omp-lswtss-c-api1-scraper-engine.dll"
                    )
                ),
                EngineAssemblyRuntimeConfigPath = Path.GetFullPath(
                    Path.Combine(
                        GetCApi1ScraperEnginePackageDirPath.Execute(),
                        "bin",
                        "Release",
                        "net8.0",
                        "omp-lswtss-c-api1-scraper-engine.runtimeconfig.json"
                    )
                ),
                EngineClassName = "OMP.LSWTSS.CApi1.ScraperEngine"
            }
        );

        RunGame.Execute(gameDirPath, gameKind);
    }
}