using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static readonly unsafe CFuncHook1<Collectables.Table.AddEntry2Method.NativeDelegate> _cApi1CollectablesTableAddEntry2MethodHook = new(
        Collectables.Table.AddEntry2Method.Info.NativePtr,
        (
            nativeDataRawPtr,
            arg0,
            arg1,
            arg2
        ) =>
        {
            var arg0AsString = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(arg0));

            foreach (var customCharacterInfo in _customCharactersInfo!)
            {
                if (customCharacterInfo.Id == arg0AsString)
                {
                    var customCharacterCApi1Guid = GetCustomCharacterCApi1Guid(customCharacterInfo.Id);

                    NuGuid.NativeHandle customCharacterCApi1GuidNativeHandle = &customCharacterCApi1Guid; 

                    var result = _cApi1CollectablesTableAddEntry2MethodHook!.Trampoline!(
                        nativeDataRawPtr,
                        arg0,
                        customCharacterCApi1GuidNativeHandle,
                        arg2
                    );

                    return result;
                }
            }

            try
            {
                foreach (var customVehicleInfo in _customVehiclesInfo)
                {
                    if (customVehicleInfo.EntryName == arg0AsString)
                    {
                        var customVehicleCApi1Guid = GetCustomCharacterCApi1Guid(customVehicleInfo.EntryName);

                        NuGuid.NativeHandle customVehicleCApi1GuidNativeHandle = &customVehicleCApi1Guid;

                        return _cApi1CollectablesTableAddEntry2MethodHook!.Trampoline!(
                            nativeDataRawPtr,
                            arg0,
                            customVehicleCApi1GuidNativeHandle,
                            arg2
                        );
                    }
                }
            }
            catch (System.Exception e)
            {
                VehicleLog($"EXCEPTION in AddEntry2 hook: {e}");
            }

            return _cApi1CollectablesTableAddEntry2MethodHook!.Trampoline!(nativeDataRawPtr, arg0, arg1, arg2);
        }
    );
}