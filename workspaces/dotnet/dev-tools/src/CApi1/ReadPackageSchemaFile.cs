using System;

namespace OMP.LSWTSS.CApi1;

public static class ReadPackageSchemaFile
{
    public static Schema Execute(string packageDirPath, string packageSchemaFileName)
    {
        var packageSchemaFilePath = System.IO.Path.Combine(packageDirPath, "src", packageSchemaFileName);

        var packageSchema = Newtonsoft.Json.JsonConvert.DeserializeObject<Schema>(
            System.IO.File.ReadAllText(packageSchemaFilePath),
            new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            }
        ) ?? throw new InvalidOperationException();

        return packageSchema;
    }
}