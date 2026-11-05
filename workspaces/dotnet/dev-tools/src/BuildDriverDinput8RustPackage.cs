namespace OMP.LSWTSS;

public static class BuildDriverDinput8RustPackage
{
    public static void Execute()
    {
        BuildRustCrate.Execute(
            System.IO.Path.Combine(
                GetRustWorkspaceDirPath.Execute(),
                "driver-dinput8-library"
            )
        );
    }
}