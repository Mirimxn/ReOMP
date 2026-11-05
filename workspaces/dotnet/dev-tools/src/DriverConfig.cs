using Newtonsoft.Json;

namespace OMP.LSWTSS;

public class DriverConfig
{
    [JsonProperty("engineAssemblyName")]
    public required string EngineAssemblyName { get; set; }

    [JsonProperty("engineAssemblyPath")]
    public required string EngineAssemblyPath { get; set; }

    [JsonProperty("engineAssemblyRuntimeConfigPath")]
    public required string EngineAssemblyRuntimeConfigPath { get; set; }

    [JsonProperty("engineClassName")]
    public required string EngineClassName { get; set; }
}