using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

public partial class V1
{
    public static CApi1.ApiWorld.NativeHandle GetCApi1CurrentApiWorld()
    {
        var cApi1CurrentApiWorld = (CApi1.ApiWorld.NativeHandle)Marshal.ReadIntPtr(
            CApi1.Native.GetPtrFromCurrentRuntimeOffset(
                steamRuntimeOffset: 0x5f129f8,
                egsRuntimeOffset: 0x5f13308
            )
        );

        if (cApi1CurrentApiWorld == nint.Zero || cApi1CurrentApiWorld.CurrentState != 2)
        {
            return (CApi1.ApiWorld.NativeHandle)nint.Zero;
        }

        return cApi1CurrentApiWorld;
    }
}