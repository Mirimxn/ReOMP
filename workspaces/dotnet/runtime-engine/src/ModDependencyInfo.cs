using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModDependencyInfo
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
    }
}