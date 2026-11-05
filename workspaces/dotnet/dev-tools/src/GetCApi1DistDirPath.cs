using System.IO;

namespace OMP.LSWTSS;

public static class GetCApi1DistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "c-api1"
        );
    }
}