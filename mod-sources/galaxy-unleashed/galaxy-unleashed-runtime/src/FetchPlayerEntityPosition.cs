using System.Numerics;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    Vector3? FetchPlayerEntityPosition()
    {
        var playerEntity = FetchPlayerEntity();

        if (playerEntity == nint.Zero)
        {
            return null;
        }

        return FetchEntityPosition(playerEntity);
    }
}