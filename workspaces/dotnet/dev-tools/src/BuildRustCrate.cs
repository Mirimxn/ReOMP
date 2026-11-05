using System;
using System.Diagnostics;

namespace OMP.LSWTSS;

public static class BuildRustCrate
{
    public static void Execute(string rustCrateDirPath)
    {
        var cargoProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C cargo build --release",
                WorkingDirectory = rustCrateDirPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        cargoProcess.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
        cargoProcess.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);

        cargoProcess.Start();

        cargoProcess.BeginOutputReadLine();
        cargoProcess.BeginErrorReadLine();

        cargoProcess.WaitForExit();

        if (cargoProcess.ExitCode != 0)
        {
            Console.Error.WriteLine($"cargo build failed (exit code {cargoProcess.ExitCode}) in {rustCrateDirPath}");
            Environment.Exit(cargoProcess.ExitCode);
        }
    }
}