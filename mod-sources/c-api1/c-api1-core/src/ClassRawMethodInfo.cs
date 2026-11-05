namespace OMP.LSWTSS.CApi1;

public sealed class ClassRawMethodInfo(uint nativeSteamRuntimeOffset, uint nativeEGSRuntimeOffset, bool isStatic) : IClassRawMethodInfo
{
    public nint NativePtr { get; private set; } = Native.GetPtrFromCurrentRuntimeOffset(
        steamRuntimeOffset: nativeSteamRuntimeOffset,
        egsRuntimeOffset: nativeEGSRuntimeOffset
    );

    public bool IsStatic { get; private set; } = isStatic;
}
