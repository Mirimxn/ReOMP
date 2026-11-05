using System.Linq;

namespace OMP.LSWTSS.CApi1;

public sealed class RegisteredVirtualClassInfo(string apiClassName, uint nativeVtableSteamRuntimeOffset, uint nativeVtableEGSRuntimeOffset) : IRegisteredVirtualClassInfo
{
    public string ApiClassName { get; private set; } = apiClassName;

    ApiClass.NativeHandle _cachedApiClass;

    public ApiClass.NativeHandle GetApiClass()
    {
        if (_cachedApiClass == nint.Zero)
        {
            _cachedApiClass = ApiClass.Instances.FirstOrDefault(apiClass => apiClass.AsApiType().GetName() == ApiClassName);
        }

        return _cachedApiClass;
    }

    public nint NativeVtablePtr { get; private set; } = Native.GetPtrFromCurrentRuntimeOffset(
        steamRuntimeOffset: nativeVtableSteamRuntimeOffset,
        egsRuntimeOffset: nativeVtableEGSRuntimeOffset
    );
}
