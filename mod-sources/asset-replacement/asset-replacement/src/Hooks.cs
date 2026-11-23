using System;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

// The game's file hooks, moved here from v1 so one mod owns them.
public static partial class AssetReplacement
{
    static CApi1.NuFileDeviceDat.NativeHandle _datDevice;
    static CApi1.NttFile.FileDevicePC.NativeHandle _hostDevice;

    static readonly CFuncHook1<CApi1.NuFileDeviceDat.ConstructorMethod.NativeDelegate> _datDeviceConstructorHook = new(
        CApi1.NuFileDeviceDat.ConstructorMethod.Info.NativePtr,
        (nativeDataRawPtr) =>
        {
            _datDevice = (CApi1.NuFileDeviceDat.NativeHandle)nativeDataRawPtr;
            Log("acquired the archive file device");
            return _datDeviceConstructorHook!.Trampoline!(nativeDataRawPtr);
        }
    );

    static readonly CFuncHook1<NttFile.FileDevicePC.ConstructorMethod.NativeDelegate> _hostDeviceConstructorHook = new(
        NttFile.FileDevicePC.ConstructorMethod.Info.NativePtr,
        (nativeDataRawPtr, arg0, arg1) =>
        {
            if (Marshal.PtrToStringUTF8(arg0) == "host:")
            {
                _hostDevice = (NttFile.FileDevicePC.NativeHandle)nativeDataRawPtr;
                Log("acquired the game folder file device");
            }
            return _hostDeviceConstructorHook!.Trampoline!(nativeDataRawPtr, arg0, arg1);
        }
    );

    // A routed or hidden path is opened from the game folder. For a hidden path nothing is
    // there, so the open fails as if the file didn't exist.
    static readonly CFuncHook1<CApi1.NuFileDeviceDat.CreateFileMethod.NativeDelegate> _createFileHook = new(
        CApi1.NuFileDeviceDat.CreateFileMethod.Info.NativePtr,
        (nativeDataRawPtr, arg0, arg1, arg2) =>
        {
            if (nativeDataRawPtr == _datDevice)
            {
                var path = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(arg1));
                if (path != null)
                {
                    NotifyRequest(path);
                    var canon = CanonPath(path);
                    if (IsHidden(canon) || IsRegistered(canon))
                    {
                        return _hostDevice.CreateFile(arg0, arg1, arg2);
                    }
                }
            }
            return _createFileHook!.Trampoline!(nativeDataRawPtr, arg0, arg1, arg2);
        }
    );

    static readonly CFuncHook1<CApi1.NuFileDeviceDat.FileGetPositionMethod.NativeDelegate> _getPositionHook = new(
        CApi1.NuFileDeviceDat.FileGetPositionMethod.Info.NativePtr,
        (nativeDataRawPtr, arg0, arg1) =>
        {
            if (nativeDataRawPtr == _datDevice)
            {
                var path = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(arg0));
                if (path != null)
                {
                    NotifyRequest(path);
                    var canon = CanonPath(path);
                    if (IsHidden(canon))
                    {
                        return 0;
                    }
                    if (IsRegistered(canon))
                    {
                        return 1;
                    }
                }
            }
            return _getPositionHook!.Trampoline!(nativeDataRawPtr, arg0, arg1);
        }
    );

    // The game checks its archive index before loading some resources (e.g. vehicle
    // previews). Files added by mods aren't in that index, so report them as existing.
    static readonly CFuncHook1<NttResourceHandle.IsFileExistsMethod.NativeDelegate> _isFileExistsHook = new(
        NttResourceHandle.IsFileExistsMethod.Info.NativePtr,
        (nativeDataRawPtr) =>
        {
            var result = _isFileExistsHook!.Trampoline!(nativeDataRawPtr);
            if (result || nativeDataRawPtr == nint.Zero || !HasRegistrations)
            {
                return result;
            }
            try
            {
                var path = ((NttResourceHandle.NativeHandle)nativeDataRawPtr).ResourcePath1;
                if (!string.IsNullOrEmpty(path) && IsRegistered(path))
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
