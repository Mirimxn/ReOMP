namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class CreateNpcSpawnersModeState : ModeState
    {
        public readonly ModeKind Kind = ModeKind.CreateNpcSpawnersMode;

        public required CreateNpcSpawnersModeConfig Config;
    }
}