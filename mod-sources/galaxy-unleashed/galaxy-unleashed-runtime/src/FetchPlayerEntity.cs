using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    ApiEntity.NativeHandle FetchPlayerEntity()
    {
        var currentApiWorld = V1.GetCApi1CurrentApiWorld();

        if (currentApiWorld == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        var nttUniverse = currentApiWorld.GetUniverse();

        if (nttUniverse == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        var playerControlSystem = NttUniverseSystemT__PlayerControlSystem.GetFrom(nttUniverse);

        if (playerControlSystem == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        return PlayerControlSystem.GetPlayerEntityForPlayerIdx(playerControlSystem, 0);
    }
}