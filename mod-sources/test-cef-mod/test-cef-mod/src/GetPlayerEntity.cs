using System;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod : IDisposable
{
    static ApiEntity.NativeHandle GetPlayerEntity()
    {
        var currentApiWorld = V1.GetCApi1CurrentApiWorld();

        if (currentApiWorld == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        var currentApiWorldUniverse = currentApiWorld.GetUniverse();

        if (currentApiWorldUniverse == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        var playerControlSystem = NttUniverseSystemT__PlayerControlSystem.GetFrom(currentApiWorldUniverse);

        if (playerControlSystem == nint.Zero)
        {
            return (ApiEntity.NativeHandle)nint.Zero;
        }

        return PlayerControlSystem.GetPlayerEntityForPlayerIdx(playerControlSystem, 0);
    }
}