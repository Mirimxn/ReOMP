namespace OMP.LSWTSS;

public static class GenerateCApi1CoreDotnetPackageSrcFiles
{
    public static void Execute()
    {
        CApi1.GeneratePackageSrcFiles.Execute(
            GetCApi1CoreDotnetPackageDirPath.Execute(),
            CApi1.ReadPackageSchemaFile.Execute(
                GetCApi1CoreDotnetPackageDirPath.Execute(),
                "CoreSchema.json"
            )
        );
    }
}