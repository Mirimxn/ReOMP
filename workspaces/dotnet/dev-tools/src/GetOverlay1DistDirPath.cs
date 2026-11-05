using System.IO;

namespace OMP.LSWTSS;

public static class GetOverlay1DistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "overlay1"
        );
    }
}