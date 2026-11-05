using System;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

static class CFuncHook1Native
{
    [DllImport("omp-lswtss-c-func-hook1-native.dll", EntryPoint = "createOMPLSWTSSCFuncHook")]
    internal static extern nint Create(nint originalPtr, nint detourPtr);

    [DllImport("omp-lswtss-c-func-hook1-native.dll", EntryPoint = "enableOMPLSWTSSCFuncHook")]
    internal static extern nint Enable(nint handle);

    [DllImport("omp-lswtss-c-func-hook1-native.dll", EntryPoint = "disableOMPLSWTSSCFuncHook")]
    internal static extern void Disable(nint handle);

    [DllImport("omp-lswtss-c-func-hook1-native.dll", EntryPoint = "destroyOMPLSWTSSCFuncHook")]
    internal static extern void Destroy(nint handle);
}

public sealed class CFuncHook1<T> : IDisposable where T : notnull
{
    readonly GCHandle _detourGcHandle;

    readonly IntPtr _handle;

    T? _trampoline;

    bool _isDisposed;

    public T? Trampoline => _trampoline;

    public CFuncHook1(nint originalPtr, T detour)
    {
        _detourGcHandle = GCHandle.Alloc(detour);
        _handle = CFuncHook1Native.Create(
            originalPtr,
            Marshal.GetFunctionPointerForDelegate(detour)
        );
    }

    public void Enable()
    {
        if (_trampoline != null)
        {
            return;
        }

        _trampoline = Marshal.GetDelegateForFunctionPointer<T>(CFuncHook1Native.Enable(_handle));
    }

    public void Disable()
    {
        if (_trampoline == null)
        {
            return;
        }

        CFuncHook1Native.Disable(_handle);

        _trampoline = default;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            try
            {
                CFuncHook1Native.Disable(_handle);
                // TODO: It causes crash
                // NativeFuncHookBinding.Destroy(handle);
            }
            catch
            {
            }

            try
            {
                _detourGcHandle.Free();
            }
            catch
            {
            }

            _isDisposed = true;
        }
    }
}