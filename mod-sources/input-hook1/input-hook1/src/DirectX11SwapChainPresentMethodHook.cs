namespace OMP.LSWTSS;

partial class InputHook1
{
    readonly static CFuncHook1<DirectX11Bindings.SwapChainPresentMethodNativeDelegate> _directX11SwapChainPresentMethodHook = new(
        DirectX11Bindings.SwapChainPresentMethodNativePtr,
        (swapChainNativeHandle, syncInterval, flags) =>
            _directX11SwapChainPresentMethodHook!.Trampoline!(swapChainNativeHandle, syncInterval, flags)
    );
}