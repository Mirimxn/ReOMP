using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static System.Numerics.Vector3 ToVector3(this Vec3.NativeData @this)
    {
        return new System.Numerics.Vector3(@this.X, @this.Y, @this.Z);
    }

    public static Vec3.NativeData ToVec3(this System.Numerics.Vector3 @this)
    {
        return new Vec3.NativeData
        {
            X = @this.X,
            Y = @this.Y,
            Z = @this.Z
        };
    }
}