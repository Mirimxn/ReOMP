namespace OMP.LSWTSS;

public static partial class V1
{
    public class CharacterInfo
    {
        public required string NameStringId { get; set; }

        public required string DescriptionStringId { get; set; }

        public required CharacterClass Class { get; set; }

        public required string PrefabResourcePath { get; set; }
    }
}