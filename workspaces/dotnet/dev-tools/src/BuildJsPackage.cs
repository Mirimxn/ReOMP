using System;
using System.Diagnostics;

namespace OMP.LSWTSS;

public static class BuildJsPackage
{
    public static void Execute(string jsPackageDirPath)
    {
        var dotnetProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C yarn && yarn build",
                WorkingDirectory = jsPackageDirPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        dotnetProcess.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
        dotnetProcess.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);

        dotnetProcess.Start();

        dotnetProcess.BeginOutputReadLine();
        dotnetProcess.BeginErrorReadLine();

        dotnetProcess.WaitForExit();

        if (dotnetProcess.ExitCode != 0)
        {
            throw new Exception($"yarn build failed (exit code {dotnetProcess.ExitCode}) in {jsPackageDirPath}");
        }
    }
}