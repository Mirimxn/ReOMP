namespace OMP.LSWTSS;

public static partial class V1
{
    static bool FetchIsDiskResourceRegistered(string diskResourceCanonPath)
    {
        return AssetReplacement.IsRegistered(diskResourceCanonPath);
    }
}
