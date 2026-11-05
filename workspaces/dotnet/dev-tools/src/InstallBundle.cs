using System.IO;

namespace OMP.LSWTSS;

public static class InstallBundle
{
    public static void Execute(string gameDirPath)
    {
        if (File.Exists(Path.Combine(gameDirPath, "omp-lswtss-driver.dll")))
        {
            File.Delete(Path.Combine(gameDirPath, "omp-lswtss-driver.dll"));
        }

        if (File.Exists(Path.Combine(gameDirPath, "dinput8.dll")))
        {
            File.Delete(Path.Combine(gameDirPath, "dinput8.dll"));
        }

        if (File.Exists(Path.Combine(gameDirPath, "omp-lswtss-driver-config.json")))
        {
            File.Delete(Path.Combine(gameDirPath, "omp-lswtss-driver-config.json"));
        }

        if (Directory.Exists(Path.Combine(gameDirPath, BundleRuntimeEngineDirName.Value)))
        {
            Directory.Delete(Path.Combine(gameDirPath, BundleRuntimeEngineDirName.Value), true);
        }

        CopyDirectory.IO.CopyDirectory(
            GetBundleDistDirPath.Execute(),
            gameDirPath,
            true
        );
    }
}