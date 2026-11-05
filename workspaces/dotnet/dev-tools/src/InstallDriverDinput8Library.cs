namespace OMP.LSWTSS;

public static class InstallDriverDinput8Library
{
    public static void Execute(string gameDirPath)
    {
        System.IO.File.Copy(
            System.IO.Path.Combine(GetRustWorkspaceDirPath.Execute(), "target", "release", "omp_lswtss_driver_dinput8_library.dll"),
            System.IO.Path.Combine(gameDirPath, "dinput8.dll"),
            true
        );
    }
}