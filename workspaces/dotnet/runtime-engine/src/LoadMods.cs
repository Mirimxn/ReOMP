namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void LoadMods()
    {
        foreach (var modState in _modsState)
        {
            LoadModIfUnloaded(modState);
        }
    }
}