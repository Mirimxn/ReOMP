using System;
using System.Numerics;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod : IDisposable
{
    static Vector3? GetEntityPosition(ApiEntity.NativeHandle entity)
    {
        if (entity == nint.Zero)
        {
            return null;
        }

        var entityTransformComponent = (ApiTransformComponent.NativeHandle)entity.FindComponentByTypeName(ApiTransformComponent.Info.ApiClassName);

        if (entityTransformComponent == nint.Zero)
        {
            return null;
        }

        return entityTransformComponent.PositionNativeData.ToVector3();
    }
}