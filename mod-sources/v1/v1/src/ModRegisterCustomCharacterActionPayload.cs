using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    class ModRegisterCustomCharacterActionPayload
    {
        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("nameStringId")]
        public required string NameStringId { get; set; }

        [JsonProperty("descriptionStringId")]
        public required string DescriptionStringId { get; set; }

        [JsonProperty("class")]
        public required CustomCharacterClass Class { get; set; }

        [JsonProperty("prefabResourcePath")]
        public required string PrefabResourcePath { get; set; }

        [JsonProperty("previewPrefabResourcePath")]
        public required string PreviewPrefabResourcePath { get; set; }
    }
}