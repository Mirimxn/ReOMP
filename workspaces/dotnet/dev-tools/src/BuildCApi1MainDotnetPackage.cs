namespace OMP.LSWTSS;

public static class BuildCApi1MainDotnetPackage
{
    public static void Execute(string steamGameDirPath, string egsGameDirPath)
    {
        BuildCApi1CoreDotnetPackage.Execute();

        RunCApi1Scraper.Execute(steamGameDirPath, GameKind.SteamGame);

        RunCApi1Scraper.Execute(egsGameDirPath, GameKind.EGSGame);

        GenerateCApi1MainDotnetPackageSrcFiles.Execute();

        BuildDotnetPackage.Execute(
            GetCApi1MainDotnetPackageDirPath.Execute()
        );
    }
}