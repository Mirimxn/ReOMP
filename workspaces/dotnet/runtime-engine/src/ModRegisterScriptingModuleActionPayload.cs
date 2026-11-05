using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModRegisterScriptingModuleActionPayload
    {
        [JsonProperty("typeName")]
        public required string TypeName { get; set; }

        [JsonProperty("assemblyPath")]
        public required string AssemblyPath { get; set; }
    }
}