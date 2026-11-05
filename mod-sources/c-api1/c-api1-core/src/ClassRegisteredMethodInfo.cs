using System.Linq;

namespace OMP.LSWTSS.CApi1;

public sealed class ClassRegisteredMethodInfo(IRegisteredClassInfo class_, string apiFunctionName, uint nativeSteamRuntimeOffset, uint nativeEGSRuntimeOffset, bool isStatic) : IClassRegisteredMethodInfo
{
    public IRegisteredClassInfo Class { get; private set; } = class_;

    public string ApiFunctionName { get; private set; } = apiFunctionName;

    ApiFunction.NativeHandle _cachedApiFunction;

    public ApiFunction.NativeHandle GetApiFunction()
    {
        if (_cachedApiFunction == nint.Zero)
        {
            var apiClass = Class.GetApiClass();

            if (apiClass == nint.Zero)
            {
                return (ApiFunction.NativeHandle)nint.Zero;
            }

            _cachedApiFunction = apiClass.GetFunctions().FirstOrDefault(apiFunction => apiFunction.GetName() == ApiFunctionName);
        }

        return _cachedApiFunction;
    }

    public nint NativePtr { get; private set; } = Native.GetPtrFromCurrentRuntimeOffset(
            steamRuntimeOffset: nativeSteamRuntimeOffset,
            egsRuntimeOffset: nativeEGSRuntimeOffset
        );

    public bool IsStatic { get; private set; } = isStatic;
}
