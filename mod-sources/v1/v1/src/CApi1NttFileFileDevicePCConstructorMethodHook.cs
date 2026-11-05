using System;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static readonly CFuncHook1<NttFile.FileDevicePC.ConstructorMethod.NativeDelegate> _cApi1NttFileFileDevicePCConstructorMethodHook = new(
        NttFile.FileDevicePC.ConstructorMethod.Info.NativePtr,
        (nativeDataRawPtr, arg0, arg1) =>
        {
            var arg0AsString = Marshal.PtrToStringUTF8(arg0);

            if (arg0AsString == "host:")
            {
                Console.WriteLine("Acquired NttFileHostFileDevicePC");
                _cApi1NttFileHostFileDevicePC = (NttFile.FileDevicePC.NativeHandle)nativeDataRawPtr;
            }

            return _cApi1NttFileFileDevicePCConstructorMethodHook!.Trampoline!(nativeDataRawPtr, arg0, arg1);
        }
    );
}