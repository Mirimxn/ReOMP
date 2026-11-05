using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static class InstallDriverConfig
{
    public static void Execute(string gameDirPath, DriverConfig driverConfig)
    {
        var driverConfigAsJson = JsonConvert.SerializeObject(driverConfig, Formatting.Indented);

        System.IO.File.WriteAllText(
            System.IO.Path.Combine(gameDirPath, "omp-lswtss-driver-config.json"),
            driverConfigAsJson
        );
    }
}