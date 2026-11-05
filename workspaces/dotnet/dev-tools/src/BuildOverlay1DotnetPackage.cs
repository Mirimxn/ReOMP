namespace OMP.LSWTSS;

public static class BuildOverlay1DotnetPackage
{
    public static void Execute()
    {
        BuildDotnetPackage.Execute(
            GetOverlay1DotnetPackageDirPath.Execute(),
            "x64"
        );
    }
}