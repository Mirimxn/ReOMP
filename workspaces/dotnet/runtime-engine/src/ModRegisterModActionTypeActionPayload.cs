using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModRegisterModActionTypeActionPayload
    {
        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("executeModActionSharedAssemblyName")]
        public required string ExecuteModActionSharedAssemblyName { get; set; }

        [JsonProperty("executeModActionStaticTypeName")]
        public required string ExecuteModActionStaticTypeName { get; set; }
    }
}