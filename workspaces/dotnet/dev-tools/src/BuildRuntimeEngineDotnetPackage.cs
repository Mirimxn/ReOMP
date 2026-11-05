namespace OMP.LSWTSS;

public static class BuildRuntimeEngineDotnetPackage
{
    public static void Execute()
    {
        BuildDotnetPackage.Execute(
            GetRuntimeEngineDotnetPackageDirPath.Execute()
        );
    }
}