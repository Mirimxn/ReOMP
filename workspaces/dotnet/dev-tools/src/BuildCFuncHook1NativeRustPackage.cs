namespace OMP.LSWTSS;

public static class BuildCFuncHook1NativeRustPackage
{
    public static void Execute()
    {
        BuildRustCrate.Execute(
            GetCFuncHook1NativeRustPackageDirPath.Execute()
        );
    }
}