namespace OMP.LSWTSS;

public static partial class V1
{
    static string GetResourceCanonPath(string resourcePath)
    {
        return resourcePath.ToLower().Replace('\\', '/');
    }
}