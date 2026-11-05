namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class NpcPresetConfig
    {
        #pragma warning disable CS0649
        public required string PrefabResourcePath;

        public required bool IsBattleParticipant;

        public required NpcFactionId? OverrideFactionId;
        #pragma warning restore CS0649
    }
}