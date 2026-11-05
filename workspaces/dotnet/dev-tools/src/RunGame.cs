using System;
using System.Diagnostics;
using System.IO;

namespace OMP.LSWTSS;

public static class RunGame
{
    public static void Execute(string gameDirPath, GameKind gameKind, string[]? gameProcessEnvExtraVars = null)
    {
        var gameProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(gameDirPath, "LEGOSTARWARSSKYWALKERSAGA_DX11.exe"),
                WorkingDirectory = gameDirPath,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
            }
        };

        if (gameKind == GameKind.SteamGame)
        {
            gameProcess.StartInfo.Environment["SteamClientLaunch"] = "1";
            gameProcess.StartInfo.Environment["SteamNoOverlayUI"] = "1";
            gameProcess.StartInfo.Environment["SteamGameId"] = "920210";
            gameProcess.StartInfo.Environment["SteamAppId"] = "920210";
            gameProcess.StartInfo.Environment["SteamOverlayGameId"] = "920210";
            gameProcess.StartInfo.Environment["SteamEnv"] = "1";
        }
        else if (gameKind == GameKind.EGSGame)
        {
            gameProcess.StartInfo.ArgumentList.Add("-epicapp=c390e58246ea4a778acd26473a489b48");
        }

        if (gameProcessEnvExtraVars != null)
        {
            foreach (var gameProcessEnvExtraVar in gameProcessEnvExtraVars)
            {
                gameProcess.StartInfo.Environment[gameProcessEnvExtraVar] = "1";
            }
        }

        gameProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Console.WriteLine(e.Data);
            }
        };

        gameProcess.Start();

        gameProcess.BeginOutputReadLine();

        gameProcess.WaitForExit();
    }
}