namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class QuickSpawnNpcsModeState : ModeState
    {
        public readonly ModeKind Kind = ModeKind.QuickSpawnNpcsMode;

        public required QuickSpawnNpcsModeConfig Config;
    }
}