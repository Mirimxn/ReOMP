namespace OMP.LSWTSS.CApi1;

public sealed class VirtualClassInfo(uint nativeVtableSteamRuntimeOffset, uint nativeVtableEGSRuntimeOffset) : IVirtualClassInfo
{
    public nint NativeVtablePtr { get; private set; } = Native.GetPtrFromCurrentRuntimeOffset(
        steamRuntimeOffset: nativeVtableSteamRuntimeOffset,
        egsRuntimeOffset: nativeVtableEGSRuntimeOffset
    );
}
