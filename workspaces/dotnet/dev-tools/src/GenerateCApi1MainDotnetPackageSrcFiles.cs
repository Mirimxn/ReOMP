namespace OMP.LSWTSS;

public static class GenerateCApi1MainDotnetPackageSrcFiles
{
    public static void Execute()
    {
        var cApi1Schema = CApi1.ReadPackageSchemaFile.Execute(
            GetCApi1MainDotnetPackageDirPath.Execute(),
            "SteamSchema.json"
        );

        CApi1.MergeSchemas.Execute(
            cApi1Schema,
            CApi1.ReadPackageSchemaFile.Execute(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "EGSSchema.json"
            ),
            false
        );

        CApi1.MergeSchemas.Execute(
            cApi1Schema,
            CApi1.ReadPackageSchemaFile.Execute(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "CustomSchema.json"
            ),
            true
        );

        CApi1.MergeSchemas.Execute(
            cApi1Schema,
            CApi1.ReadPackageSchemaFile.Execute(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "SteamVtableSchema.json"
            ),
            false
        );

        CApi1.MergeSchemas.Execute(
            cApi1Schema,
            CApi1.ReadPackageSchemaFile.Execute(
                GetCApi1MainDotnetPackageDirPath.Execute(),
                "EGSVtableSchema.json"
            ),
            false
        );

        CApi1.GeneratePackageSrcFiles.Execute(
            GetCApi1MainDotnetPackageDirPath.Execute(),
            cApi1Schema
        );
    }
}