using System;
using System.IO;
using System.IO.Compression;

namespace OMP.LSWTSS;

public static class PackageMod
{
    public static void Execute(string modId, string outputDirectory)
    {
        var source = GetModSourceDirPath.Execute(modId);
        File.Copy(Path.Combine(source, "mod.json"), Path.Combine(outputDirectory, "mod.json"), overwrite: true);

        var documentation = Path.Combine(source, "mod.md");
        var packagedDocumentation = Path.Combine(outputDirectory, "mod.md");
        if (File.Exists(documentation))
            File.Copy(documentation, packagedDocumentation, overwrite: true);
        else
            File.Delete(packagedDocumentation);

        var packages = Path.Combine(GetDistDirPath.Execute(), "mod-packages");
        Directory.CreateDirectory(packages);
        var temporary = Path.Combine(packages, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            ZipFile.CreateFromDirectory(outputDirectory, temporary, CompressionLevel.Fastest, includeBaseDirectory: true);
            File.Move(temporary, Path.Combine(packages, modId + ".zip"), overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
