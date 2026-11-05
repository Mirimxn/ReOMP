using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModActionInfo
    {
        [JsonProperty("typeId")]
        public required string TypeId { get; set; }

        [JsonProperty("payload")]
        public required JObject Payload { get; set; }
    }
}