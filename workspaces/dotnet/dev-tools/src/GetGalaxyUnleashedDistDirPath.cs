using System.IO;

namespace OMP.LSWTSS;

public static class GetGalaxyUnleashedDistDirPath
{
    public static string Execute()
    {
        return Path.Combine(
            GetDistDirPath.Execute(),
            "galaxy-unleashed"
        );
    }
}