namespace OMP.LSWTSS;

public static class BuildInputHook1DotnetPackage
{
    public static void Execute()
    {
        BuildDotnetPackage.Execute(
            GetInputHook1DotnetPackageDirPath.Execute()
        );
    }
}