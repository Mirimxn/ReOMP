namespace OMP.LSWTSS;

public static class GetInputHook1DotnetPackageDirPath
{
    public static string Execute()
    {
        return GetModSourceDirPath.Execute("input-hook1", "input-hook1");
    }
}