namespace OMP.LSWTSS.CApi1;

public class Manager
{
    static readonly CFuncHook1<ApiRegistry.RegisterApiClassMethod.NativeDelegate> _apiRegistryRegisterApiClassMethodHook = new(
        ApiRegistry.RegisterApiClassMethod.Info.NativePtr,
        (
            nint nativeDataRawPtr,
            nint nameNativeDataRawPtr,
            nint apiClassInterfaceNativeDataRawPtr,
            nint param3,
            nint edFileDescDataRawPtr,
            nint param5,
            float param6,
            nint param7,
            int edClassSerializationTarget
        ) =>
        {
            var apiClassNativeDataRawPtr = _apiRegistryRegisterApiClassMethodHook!.Trampoline!(
                nativeDataRawPtr,
                nameNativeDataRawPtr,
                apiClassInterfaceNativeDataRawPtr,
                param3,
                edFileDescDataRawPtr,
                param5,
                param6,
                param7,
                edClassSerializationTarget
            );

            var apiClass = (ApiClass.NativeHandle)apiClassNativeDataRawPtr;

            ApiClass.Instances.Add(apiClass);

            return apiClassNativeDataRawPtr;
        }
    );

    public Manager()
    {
        _apiRegistryRegisterApiClassMethodHook.Enable();
    }
}