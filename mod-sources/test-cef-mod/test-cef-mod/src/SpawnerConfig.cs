using System;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class SpawnerConfig
    {
        #pragma warning disable CS0649
        public required NpcPresetConfig NpcPreset;

        public required uint NpcsMaxCount;

        public required float SpawnNpcTasksIntervalAsSeconds;
        #pragma warning restore CS0649

    }
}