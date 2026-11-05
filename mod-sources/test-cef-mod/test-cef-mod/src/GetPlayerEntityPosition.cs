using System;
using System.Numerics;

namespace OMP.LSWTSS;

public partial class TestCefMod : IDisposable
{
    static Vector3? GetPlayerEntityPosition()
    {
        var playerEntity = GetPlayerEntity();

        if (playerEntity == nint.Zero)
        {
            return null;
        }

        return GetEntityPosition(playerEntity);
    }
}