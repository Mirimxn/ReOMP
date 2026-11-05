namespace OMP.LSWTSS;

public static class GetDotnetWorkspaceDirPath
{
    public static string Execute()
    {
        return System.IO.Path.Combine(
            System.Environment.CurrentDirectory,
            ".."
        );
    }
}