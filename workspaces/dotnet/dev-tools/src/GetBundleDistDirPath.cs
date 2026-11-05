using System.IO;

namespace OMP.LSWTSS;

public static class GetBundleDistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "bundle"
        );
    }
}