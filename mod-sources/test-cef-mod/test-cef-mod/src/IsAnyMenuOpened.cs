namespace OMP.LSWTSS;

public partial class TestCefMod
{
    static bool IsAnyMenuOpened => _quickMenuState != null || _spawnerMenuState != null;
}