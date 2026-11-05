using System.IO;

namespace OMP.LSWTSS;

public static class GetModSourceDirPath
{
    public static string Execute(string modId, string project = "")
    {
        return Path.GetFullPath(Path.Combine(GetDistDirPath.Execute(), "..", "mod-sources", modId, project));
    }
}
