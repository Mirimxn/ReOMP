using System.IO;

namespace OMP.LSWTSS;

public static class GetV1DistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "v1"
        );
    }
}