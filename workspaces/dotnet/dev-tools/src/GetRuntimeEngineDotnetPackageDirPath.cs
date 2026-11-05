namespace OMP.LSWTSS;

public static class GetRuntimeEngineDotnetPackageDirPath
{
    public static string Execute()
    {
        return System.IO.Path.Combine(
            GetDotnetWorkspaceDirPath.Execute(),
            "runtime-engine"
        );
    }
}