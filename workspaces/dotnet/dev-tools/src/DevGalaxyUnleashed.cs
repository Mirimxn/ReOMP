namespace OMP.LSWTSS;

public static class DevGalaxyUnleashed
{
    public static void Execute(string gameDirPath)
    {
        BuildGalaxyUnleashed.Execute();
        InstallAll.Execute(gameDirPath);
        
        RunGame.Execute(gameDirPath, GameKind.SteamGame, ["DEV_GALAXY_UNLEASHED"]);
    }
}