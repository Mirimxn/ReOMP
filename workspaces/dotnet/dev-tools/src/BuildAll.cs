namespace OMP.LSWTSS;

public static class BuildAll
{
    public static void Execute(string steamGameDirPath, string egsGameDirPath)
    {
        BuildBundle.Execute();
        BuildCFuncHook1.Execute();
        BuildCApi1.Execute(steamGameDirPath, egsGameDirPath);
        BuildInputHook1.Execute();
        BuildOverlay1.Execute();
        BuildAssetReplacement.Execute();
        BuildV1.Execute();
        BuildDebugTools.Execute();
        BuildTestCefMod.Execute();
        BuildGalaxyUnleashed.Execute();
    }
}
