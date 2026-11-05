using System;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Diagnostics only: with "debugLog" on, logs load requests for vehicle resources.
    static readonly CFuncHook1<NttResourceHandle.AsyncLoadMethod.NativeDelegate> _cApi1NttResourceHandleAsyncLoadMethodHook = new(
        NttResourceHandle.AsyncLoadMethod.Info.NativePtr,
        (nativeDataRawPtr) =>
        {
            var result = _cApi1NttResourceHandleAsyncLoadMethodHook!.Trampoline!(nativeDataRawPtr);

            if (_vehicleDebugLog && nativeDataRawPtr != nint.Zero)
            {
                try
                {
                    var path = ((NttResourceHandle.NativeHandle)nativeDataRawPtr).ResourcePath1;
                    if (path != null && path.Contains("chars/vehicles", StringComparison.OrdinalIgnoreCase))
                    {
                        VehicleDebug($"AsyncLoad: {path} -> {result}");
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }
    );
}
