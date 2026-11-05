using System;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    // The game checks its DAT index to decide whether a resource exists before loading it
    // (e.g. vehicle previews). Files added by mods are not in that index, so report them as existing.
    static readonly CFuncHook1<NttResourceHandle.IsFileExistsMethod.NativeDelegate> _cApi1NttResourceHandleIsFileExistsMethodHook = new(
        NttResourceHandle.IsFileExistsMethod.Info.NativePtr,
        (nativeDataRawPtr) =>
        {
            var result = _cApi1NttResourceHandleIsFileExistsMethodHook!.Trampoline!(nativeDataRawPtr);
            if (result || _diskResourcesInfo.Count == 0 || nativeDataRawPtr == nint.Zero)
            {
                return result;
            }

            try
            {
                var path = ((NttResourceHandle.NativeHandle)nativeDataRawPtr).ResourcePath1;
                if (!string.IsNullOrEmpty(path) && FetchIsDiskResourceRegistered(GetResourceCanonPath(path)))
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return result;
        }
    );
}
