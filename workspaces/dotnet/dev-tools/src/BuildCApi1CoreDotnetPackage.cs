namespace OMP.LSWTSS;

public static class BuildCApi1CoreDotnetPackage
{
    public static void Execute()
    {
        GenerateCApi1CoreDotnetPackageSrcFiles.Execute();

        BuildDotnetPackage.Execute(
            GetCApi1CoreDotnetPackageDirPath.Execute()
        );
    }
}