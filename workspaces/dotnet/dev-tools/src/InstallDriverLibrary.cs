namespace OMP.LSWTSS;

public static class InstallDriverLibrary
{
    public static void Execute(string gameDirPath)
    {
        System.IO.File.Copy(
            System.IO.Path.Combine(GetRustWorkspaceDirPath.Execute(), "target", "release", "omp_lswtss_driver_library.dll"),
            System.IO.Path.Combine(gameDirPath, "omp-lswtss-driver.dll"),
            true
        );
    }
}