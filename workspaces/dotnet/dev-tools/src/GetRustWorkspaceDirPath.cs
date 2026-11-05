namespace OMP.LSWTSS;

public static class GetRustWorkspaceDirPath
{
    public static string Execute()
    {
        return System.IO.Path.Combine(
            System.Environment.CurrentDirectory,
            "..",
            "..",
            "rust"
        );
    }
}