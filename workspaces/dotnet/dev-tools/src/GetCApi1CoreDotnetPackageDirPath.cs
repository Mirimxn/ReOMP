namespace OMP.LSWTSS;

public static class GetCApi1CoreDotnetPackageDirPath
{
    public static string Execute()
    {
        return GetModSourceDirPath.Execute("c-api1", "c-api1-core");
    }
}