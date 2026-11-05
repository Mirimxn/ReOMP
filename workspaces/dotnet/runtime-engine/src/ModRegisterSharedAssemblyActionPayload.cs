using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModRegisterSharedAssemblyActionPayload
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("path")]
        public required string Path { get; set; }
    }
}