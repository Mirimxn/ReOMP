using System.Numerics;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class BattleConfig
    {
        public required Vector3? CenterPosition;
    }

    static BattleConfig _battleConfig = new()
    {
        CenterPosition = new Vector3(1f, 2f, 3f)
    };
}