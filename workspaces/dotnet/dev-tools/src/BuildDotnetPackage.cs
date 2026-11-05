using System;
using System.Diagnostics;

namespace OMP.LSWTSS;

public static class BuildDotnetPackage
{
    public static void Execute(string dotnetPackageDirPath, string? dotnetPackageArch = null)
    {
        var dotnetProcessArchArg = dotnetPackageArch != null ? $"--arch {dotnetPackageArch}" : string.Empty;

        var dotnetProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C dotnet build --configuration Release {dotnetProcessArchArg}",
                WorkingDirectory = dotnetPackageDirPath,
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
            Console.Error.WriteLine($"dotnet build failed (exit code {dotnetProcess.ExitCode}) in {dotnetPackageDirPath}");
            Environment.Exit(dotnetProcess.ExitCode);
        }
    }
}