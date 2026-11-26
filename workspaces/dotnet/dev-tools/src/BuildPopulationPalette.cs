using System.IO;

namespace OMP.LSWTSS;

public static class BuildPopulationPalette
{
    public static string DistDirPath => Path.Combine(GetDistDirPath.Execute(), "population-palette");

    public static void Execute()
    {
        var distDirPath = DistDirPath;
        if (Directory.Exists(distDirPath))
        {
            Directory.Delete(distDirPath, true);
        }
        Directory.CreateDirectory(distDirPath);

        var projectDirPath = GetModSourceDirPath.Execute("population-palette", "population-palette");
        BuildDotnetPackage.Execute(projectDirPath);
        CopyDirectory.IO.CopyDirectory(Path.Combine(projectDirPath, "bin", "Release", "net8.0"), distDirPath);

        foreach (var leftover in new[] { "omp-lswtss-population-palette.deps.json", "omp-lswtss-population-palette.pdb" })
        {
            File.Delete(Path.Combine(distDirPath, leftover));
        }

        PackageMod.Execute("population-palette", distDirPath);
    }
}
