namespace OMP.LSWTSS;

partial class Overlay1
{
    readonly static CFuncHook1<DirectX11Bindings.SwapChainResizeBuffersMethodNativeDelegate> _directX11SwapChainResizeBuffersMethodHook = new(
        DirectX11Bindings.SwapChainResizeBuffersMethodNativePtr,
        (
            swapChainNativeHandle,
            bufferCount,
            width,
            height,
            newFormat,
            swapChainFlags
        ) =>
        {
            foreach (var instance in _instances!)
            {
                instance._directX11OverlayQuad?.Dispose();
                instance._directX11OverlayQuad = null;
            }

            return _directX11SwapChainResizeBuffersMethodHook!.Trampoline!(
                swapChainNativeHandle,
                bufferCount,
                width,
                height,
                newFormat,
                swapChainFlags
            );
        }
    );
}