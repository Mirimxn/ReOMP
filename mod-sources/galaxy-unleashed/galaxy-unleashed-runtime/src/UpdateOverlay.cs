namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    void UpdateOverlay()
    {
        _overlay.IsVisible = _modeState is not PlayModeState;
        _overlay.IsActive = _modeState is MenuModeState;
    }
}