using System.IO;

namespace OMP.LSWTSS;

public static class GetTestCefModDistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "test-cef-mod"
        );
    }
}