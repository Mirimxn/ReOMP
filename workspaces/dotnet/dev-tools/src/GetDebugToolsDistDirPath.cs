using System.IO;

namespace OMP.LSWTSS;

public static class GetDebugToolsDistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "debug-tools"
        );
    }
}