using System.IO;

namespace OMP.LSWTSS;

public static class BuildAssetReplacement
{
    public static string DistDirPath => Path.Combine(GetDistDirPath.Execute(), "asset-replacement");

    public static void Execute()
    {
        var distDirPath = DistDirPath;
        if (Directory.Exists(distDirPath))
        {
            Directory.Delete(distDirPath, true);
        }
        Directory.CreateDirectory(distDirPath);

        var projectDirPath = GetModSourceDirPath.Execute("asset-replacement", "asset-replacement");
        BuildDotnetPackage.Execute(projectDirPath);
        CopyDirectory.IO.CopyDirectory(Path.Combine(projectDirPath, "bin", "Release", "net8.0"), distDirPath);

        foreach (var leftover in new[] { "omp-lswtss-asset-replacement.deps.json", "omp-lswtss-asset-replacement.pdb" })
        {
            File.Delete(Path.Combine(distDirPath, leftover));
        }

        PackageMod.Execute("asset-replacement", distDirPath);
    }
}
