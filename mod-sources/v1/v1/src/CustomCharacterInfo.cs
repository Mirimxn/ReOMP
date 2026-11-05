namespace OMP.LSWTSS;

public static partial class V1
{
    class CustomCharacterInfo
    {
        public required string Id { get; set; }

        public required string NameStringId { get; set; }

        public required string DescriptionStringId { get; set; }

        public required CustomCharacterClass Class { get; set; }

        public required string PrefabResourcePath { get; set; }

        public required string PreviewPrefabResourcePath { get; set; }
    }
}