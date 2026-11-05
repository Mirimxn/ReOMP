using System;
using OMP.LSWTSS;

string commandName = args[0];

if (commandName == "build-c-func-hook1")
{
    BuildCFuncHook1.Execute();
}
else if (commandName == "install-c-func-hook1")
{
    string gameDirPath = args[1];
    InstallCFuncHook1.Execute(gameDirPath);
}
else if (commandName == "build-c-api1")
{
    string steamGameDirPath = args[1];
    string egsGameDirPath = args[2];
    BuildCApi1.Execute(steamGameDirPath, egsGameDirPath);
}
else if (commandName == "install-c-api1")
{
    string gameDirPath = args[1];
    InstallCApi1.Execute(gameDirPath);
}
else if (commandName == "build-input-hook1")
{
    BuildInputHook1.Execute();
}
else if (commandName == "install-input-hook1")
{
    string gameDirPath = args[1];
    InstallInputHook1.Execute(gameDirPath);
}
else if (commandName == "build-overlay1")
{
    BuildOverlay1.Execute();
}
else if (commandName == "install-overlay1")
{
    string gameDirPath = args[1];
    InstallOverlay1.Execute(gameDirPath);
}
else if (commandName == "build-debug-tools")
{
    BuildDebugTools.Execute();
}
else if (commandName == "install-debug-tools")
{
    string gameDirPath = args[1];
    InstallDebugTools.Execute(gameDirPath);
}
else if (commandName == "build-v1")
{
    BuildV1.Execute();
}
else if (commandName == "install-v1")
{
    string gameDirPath = args[1];
    InstallV1.Execute(gameDirPath);
}
else if (commandName == "build-test-cef-mod")
{
    BuildTestCefMod.Execute();
}
else if (commandName == "install-test-cef-mod")
{
    string gameDirPath = args[1];
    InstallTestCefMod.Execute(gameDirPath);
}
else if (commandName == "update-test-cef-mod")
{
    string gameDirPath = args[1];
    UpdateTestCefMod.Execute(gameDirPath);
}
else if (commandName == "build-galaxy-unleashed")
{
    BuildGalaxyUnleashed.Execute();
}
else if (commandName == "install-galaxy-unleashed")
{
    string gameDirPath = args[1];
    InstallGalaxyUnleashed.Execute(gameDirPath);
}
else if (commandName == "update-galaxy-unleashed")
{
    string gameDirPath = args[1];
    UpdateGalaxyUnleashed.Execute(gameDirPath);
}
else if (commandName == "dev-galaxy-unleashed")
{
    string gameDirPath = args[1];
    DevGalaxyUnleashed.Execute(gameDirPath);
}
else if (commandName == "dev-update-galaxy-unleashed")
{
    string gameDirPath = args[1];
    DevUpdateGalaxyUnleashed.Execute(gameDirPath);
}
else if (commandName == "build-bundle")
{
    BuildBundle.Execute();
}
else if (commandName == "install-bundle")
{
    string gameDirPath = args[1];
    InstallBundle.Execute(gameDirPath);
}
else if (commandName == "build-all")
{
    string steamGameDirPath = args[1];
    string egsGameDirPath = args[2];
    BuildAll.Execute(steamGameDirPath, egsGameDirPath);
}
else if (commandName == "package-mod")
{
    var modId = args[1];
    if (modId is not ("c-api1" or "c-func-hook1" or "debug-tools" or "galaxy-unleashed" or "input-hook1" or "overlay1" or "test-cef-mod" or "v1"))
        throw new ArgumentException($"Unknown bundled mod {modId}");
    PackageMod.Execute(modId, System.IO.Path.Combine(GetDistDirPath.Execute(), modId));
}
else if (commandName == "build-all-no-c-api1")
{
    BuildAllNoCApi1.Execute();
}
else if (commandName == "install-all")
{
    string gameDirPath = args[1];
    InstallAll.Execute(gameDirPath);
}
else if (commandName == "run-steam-game")
{
    string gameDirPath = args[1];
    RunGame.Execute(gameDirPath, GameKind.SteamGame);
}
else if (commandName == "run-egs-game")
{
    string gameDirPath = args[1];
    RunGame.Execute(gameDirPath, GameKind.EGSGame);
}
else
{
    throw new InvalidOperationException();
}
