using System.IO;

namespace OMP.LSWTSS;

public static class GetDistDirPath
{
    public static string Execute()
    {
        return Path.GetFullPath(
            Path.Combine(
                System.Environment.CurrentDirectory,
                "..",
                "..",
                "..",
                "dist"
            )
        );
    }
}