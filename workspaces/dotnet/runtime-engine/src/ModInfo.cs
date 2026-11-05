using System.Collections.Generic;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModInfo
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("actions")]
        public required List<ModActionInfo> Actions { get; set; }

        [JsonProperty("dependencies")]
        public required List<ModDependencyInfo>? Dependencies { get; set; }
    }
}
