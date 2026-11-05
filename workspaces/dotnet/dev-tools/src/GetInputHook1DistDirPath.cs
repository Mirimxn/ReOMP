using System.IO;

namespace OMP.LSWTSS;

public static class GetInputHook1DistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "input-hook1"
        );
    }
}