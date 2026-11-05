namespace OMP.LSWTSS.CApi1;

public static class GeneratePackageSrcFiles
{
    public static void Execute(string packageDirPath, Schema packageSchema)
    {
        var packageSrcGeneratedDirPath = System.IO.Path.Combine(
            packageDirPath,
            "src",
            "Generated"
        );

        if (System.IO.Directory.Exists(packageSrcGeneratedDirPath))
        {
            System.IO.Directory.Delete(packageSrcGeneratedDirPath, true);
        }

        System.IO.Directory.CreateDirectory(packageSrcGeneratedDirPath);

        foreach (var packageClassSchema in packageSchema.Classes)
        {
            var packageClassFullName = packageClassSchema.Namespace == null
                ? packageClassSchema.Name
                : $"{packageClassSchema.Namespace}.{packageClassSchema.Name}";

            var packageClassSrcFileName = $"{packageClassFullName}.cs";

            var packageClassSrcFilePath = System.IO.Path.Combine(
                packageSrcGeneratedDirPath,
                packageClassSrcFileName
            );

            System.IO.File.WriteAllText(
                packageClassSrcFilePath,
                GetClassSrc.Execute(packageClassSchema, packageSchema.Classes)
            );
        }
    }
}