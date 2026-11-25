namespace OMP.LSWTSS;

/// <summary>Scripting module entry point: enables the file hooks at startup.</summary>
public class AssetReplacementModule
{
    public AssetReplacementModule()
    {
        AssetReplacement.InitIfNotInitialized();
    }
}
