using System;

namespace OMP.LSWTSS;

public static partial class V1
{
    static readonly CFuncHook1<CApi1.NuFileDeviceDat.ConstructorMethod.NativeDelegate> _cApi1NuFileDeviceDatConstructorMethodHook = new(
        CApi1.NuFileDeviceDat.ConstructorMethod.Info.NativePtr,
        (nativeDataRawPtr) =>
        {
            _cApi1NuFileDeviceDat = (CApi1.NuFileDeviceDat.NativeHandle)nativeDataRawPtr;
            Console.WriteLine("Acquired NuFileDeviceDat");
            return _cApi1NuFileDeviceDatConstructorMethodHook!.Trampoline!(nativeDataRawPtr);
        }
    );
}