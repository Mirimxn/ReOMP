using System;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class AssetReplacement
{
    /// <summary>Handler for the <c>asset-replacement/register</c> manifest action.</summary>
    public static class RegisterAction
    {
        sealed class Payload
        {
            [JsonProperty("path")]
            public required string Path { get; set; }

            [JsonProperty("srcPath")]
            public required string SrcPath { get; set; }
        }

        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            InitIfNotInitialized();
            var payload = JsonConvert.DeserializeObject<Payload>(modActionPayloadAsJson)
                ?? throw new InvalidOperationException("asset-replacement/register needs a payload with path and srcPath");
            RegisterModFile(modId, modDirPath, payload.Path, payload.SrcPath);
        }
    }
}
