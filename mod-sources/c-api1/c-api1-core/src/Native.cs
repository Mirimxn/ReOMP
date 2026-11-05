using System.Linq;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public enum NativeRuntimeKind
{
    SteamRuntime,
    EGSRuntime,
}

public static class Native
{
    static NativeRuntimeKind? _currentRuntimeCachedKind;

    public static NativeRuntimeKind GetCurrentRuntimeKind()
    {
        if (_currentRuntimeCachedKind == null)
        {
            if (System.Environment.GetEnvironmentVariable("SteamAppId") != null)
            {
                _currentRuntimeCachedKind = NativeRuntimeKind.SteamRuntime;
            }
            else if (System.Environment.GetCommandLineArgs().Any(x => x.Contains("-epicapp")))
            {
                _currentRuntimeCachedKind = NativeRuntimeKind.EGSRuntime;
            }
            else
            {
                throw new System.InvalidOperationException();
            }
        }

        return _currentRuntimeCachedKind.Value;
    }

    public static T GetCurrentRuntimeValue<T>(
        T steamRuntimeValue,
        T egsRuntimeValue
    )
    {
        return GetCurrentRuntimeKind() switch
        {
            NativeRuntimeKind.SteamRuntime => steamRuntimeValue,
            NativeRuntimeKind.EGSRuntime => egsRuntimeValue,
            _ => throw new System.InvalidOperationException(),
        };
    }

    public static nint GetPtrFromOffset(uint offset)
    {
        var processMainModuleBaseAddress = System.Diagnostics.Process.GetCurrentProcess().MainModule!.BaseAddress;

        return nint.Add(processMainModuleBaseAddress, (int)offset);
    }

    public static uint GetOffsetFromPtr(nint ptr)
    {
        var processMainModuleBaseAddress = System.Diagnostics.Process.GetCurrentProcess().MainModule!.BaseAddress;

        return (uint)(ptr - processMainModuleBaseAddress);
    }

    public static nint GetPtrFromCurrentRuntimeOffset(
        uint steamRuntimeOffset,
        uint egsRuntimeOffset
    )
    {
        var offset = GetCurrentRuntimeValue(
            steamRuntimeValue: steamRuntimeOffset,
            egsRuntimeValue: egsRuntimeOffset
        );

        return GetPtrFromOffset(offset);
    }

    public static TMethodDelegate GetMethodDelegate<TMethodDelegate>(
        nint methodNativePtr
    ) where TMethodDelegate : notnull
    {
        return Marshal.GetDelegateForFunctionPointer<TMethodDelegate>(methodNativePtr);
    }

    public static nint GetVtableMethodPtr(
        nint vtablePtr,
        uint vtableMethodIndex
    )
    {
        return Marshal.ReadIntPtr(vtablePtr, (int)vtableMethodIndex * nint.Size);
    }

    public static TVtableMethodDelegate GetVtableMethodDelegate<TVtableMethodDelegate>(
        nint vtablePtr,
        uint vtableMethodIndex
    ) where TVtableMethodDelegate : notnull
    {
        var vtableMethodPtr = GetVtableMethodPtr(vtablePtr, vtableMethodIndex);

        return GetMethodDelegate<TVtableMethodDelegate>(vtableMethodPtr);
    }

    public static nint GetObjectVtablePtr(
        nint objectDataRawPtr
    )
    {
        return Marshal.ReadIntPtr(objectDataRawPtr);
    }

    public static TObjectVtableMethodDelegate GetObjectVtableMethodDelegate<TObjectVtableMethodDelegate>(
        nint objectDataRawPtr,
        uint objectVtableMethodIndex
    ) where TObjectVtableMethodDelegate : notnull
    {
        var objectVtablePtr = GetObjectVtablePtr(objectDataRawPtr);

        return GetVtableMethodDelegate<TObjectVtableMethodDelegate>(objectVtablePtr, objectVtableMethodIndex);
    }
}
