using System.Linq;

namespace OMP.LSWTSS;

public static partial class V1
{
    static bool FetchIsDiskResourceRegistered(string diskResourceCanonPath)
    {
        return _diskResourcesInfo.Any((diskResourceInfo) => diskResourceInfo.CanonPath == diskResourceCanonPath);
    }
}