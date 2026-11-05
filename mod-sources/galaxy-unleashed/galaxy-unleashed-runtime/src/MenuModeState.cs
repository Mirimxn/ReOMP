namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class MenuModeState : ModeState
    {
        public readonly ModeKind Kind = ModeKind.MenuMode;

        public required MenuModeConfig Config;
    }
}