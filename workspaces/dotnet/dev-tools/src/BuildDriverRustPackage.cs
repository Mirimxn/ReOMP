namespace OMP.LSWTSS;

public static class BuildDriverRustPackage
{
    public static void Execute()
    {
        BuildRustCrate.Execute(
            System.IO.Path.Combine(
                GetRustWorkspaceDirPath.Execute(),
                "driver-library"
            )
        );
    }
}