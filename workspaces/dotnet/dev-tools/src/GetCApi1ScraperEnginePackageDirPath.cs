namespace OMP.LSWTSS;

public static class GetCApi1ScraperEnginePackageDirPath
{
    public static string Execute()
    {
        return System.IO.Path.Combine(
            GetDotnetWorkspaceDirPath.Execute(),
            "c-api1-scraper-engine"
        );
    }
}