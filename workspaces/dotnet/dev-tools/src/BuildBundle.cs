using System.IO;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static class BuildBundle
{
    public static void Execute()
    {
        BuildDriverDinput8RustPackage.Execute();
        BuildDriverRustPackage.Execute();
        BuildRuntimeEngineDotnetPackage.Execute();

        var bundleDistDirPath = GetBundleDistDirPath.Execute();

        if (Directory.Exists(bundleDistDirPath))
        {
            Directory.Delete(bundleDistDirPath, true);
        }

        Directory.CreateDirectory(bundleDistDirPath);

        File.Copy(
            Path.Combine(
                GetRustWorkspaceDirPath.Execute(),
                "target",
                "release",
                "omp_lswtss_driver_dinput8_library.dll"
            ),
            Path.Combine(
                bundleDistDirPath,
                "dinput8.dll"
            ),
            true
        );

        File.Copy(
            Path.Combine(
                GetRustWorkspaceDirPath.Execute(),
                "target",
                "release",
                "omp_lswtss_driver_library.dll"
            ),
            Path.Combine(
                bundleDistDirPath,
                "omp-lswtss-driver.dll"
            ),
            true
        );

        CopyDirectory.IO.CopyDirectory(
            Path.Combine(
                GetRuntimeEngineDotnetPackageDirPath.Execute(),
                "bin",
                "Release",
                "net8.0"
            ),
            Path.Combine(
                bundleDistDirPath,
                BundleRuntimeEngineDirName.Value
            ),
            true
        );

        File.Delete(
            Path.Combine(
                bundleDistDirPath,
                BundleRuntimeEngineDirName.Value,
                "omp-lswtss-runtime-engine.deps.json"
            )
        );

        var driverConfigAsJson = JsonConvert.SerializeObject(
            new DriverConfig
            {
                EngineAssemblyName = "omp-lswtss-runtime-engine",
                EngineAssemblyPath = $"{BundleRuntimeEngineDirName.Value}\\omp-lswtss-runtime-engine.dll",
                EngineAssemblyRuntimeConfigPath = $"{BundleRuntimeEngineDirName.Value}\\omp-lswtss-runtime-engine.runtimeconfig.json",
                EngineClassName = "OMP.LSWTSS.RuntimeEngine"
            },
            Formatting.Indented
        );

        File.WriteAllText(
            Path.Combine(bundleDistDirPath, "omp-lswtss-driver-config.json"),
            driverConfigAsJson
        );
    }
}