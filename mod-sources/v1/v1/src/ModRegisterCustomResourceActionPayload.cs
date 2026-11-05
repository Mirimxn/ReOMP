using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    class ModRegisterCustomResourceActionPayload
    {
        [JsonProperty("path")]
        public required string Path { get; set; }

        [JsonProperty("srcPath")]
        public required string SrcPath { get; set; }
    }
}