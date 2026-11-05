namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class NpcSpawnerConfig
    {
        #pragma warning disable 0649
        public required int MaxNpcsCount;

        public required int NpcSpawningIntervalSeconds;

        public required NpcPresetConfig NpcPreset;

        public required bool AreNpcsBattleParticipants;
        #pragma warning restore 0649
    }
}