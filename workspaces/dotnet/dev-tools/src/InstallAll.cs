namespace OMP.LSWTSS;

public static class InstallAll
{
    public static void Execute(string gameDirPath)
    {
        InstallBundle.Execute(gameDirPath);
        InstallCFuncHook1.Execute(gameDirPath);
        InstallCApi1.Execute(gameDirPath);
        InstallInputHook1.Execute(gameDirPath);
        InstallOverlay1.Execute(gameDirPath);
        InstallV1.Execute(gameDirPath);
        InstallDebugTools.Execute(gameDirPath);
        InstallGalaxyUnleashed.Execute(gameDirPath);
    }
}