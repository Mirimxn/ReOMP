namespace OMP.LSWTSS;

public static class BuildCApi1ScraperEngineDotnetPackage
{
    public static void Execute()
    {
        BuildCFuncHook1.Execute();
        BuildCApi1CoreDotnetPackage.Execute();

        BuildDotnetPackage.Execute(
            GetCApi1ScraperEnginePackageDirPath.Execute()
        );
    }
}