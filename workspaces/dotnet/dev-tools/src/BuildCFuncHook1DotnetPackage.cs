namespace OMP.LSWTSS;

public static class BuildCFuncHook1DotnetPackage
{
    public static void Execute()
    {
        BuildDotnetPackage.Execute(
            GetCFuncHook1DotnetPackageDirPath.Execute()
        );
    }
}