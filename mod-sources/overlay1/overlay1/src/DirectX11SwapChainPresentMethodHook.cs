using System;

namespace OMP.LSWTSS;

partial class Overlay1
{
    // Set if the overlay rendering ever throws, so one failure disables the
    // overlay instead of crashing the game on every subsequent frame.
    static bool _presentHookDisabled;

    readonly static CFuncHook1<DirectX11Bindings.SwapChainPresentMethodNativeDelegate> _directX11SwapChainPresentMethodHook = new(
        DirectX11Bindings.SwapChainPresentMethodNativePtr,
        (
            swapChainNativeHandle,
            syncInterval,
            flags
        ) =>
        {
            if (!_presentHookDisabled)
            {
                try
                {
                    foreach (var instance in _instances!)
                    {
                        if (instance.IsVisible)
                        {
                            if (
                                instance._directX11OverlayQuad == null
                                ||
                                instance._directX11OverlayQuad.SwapChain.NativePointer != swapChainNativeHandle
                            )
                            {
                                instance._directX11OverlayQuad?.Dispose();

                                // AddRef before wrapping: SharpDX doesn't AddRef in the constructor,
                                // so without this the wrapper's finalizer would Release the game's
                                // only reference and destroy the swap chain.
                                System.Runtime.InteropServices.Marshal.AddRef(swapChainNativeHandle);

                                instance._directX11OverlayQuad = new DirectX11OverlayQuad(
                                    new SharpDX.DXGI.SwapChain(swapChainNativeHandle)
                                );

                                CefSharp.WebBrowserExtensions.GetBrowserHost(instance.ChromiumWebBrowser).WindowlessFrameRate = 60;

                                instance.ChromiumWebBrowser.Size = new System.Drawing.Size(
                                    instance._directX11OverlayQuad.TextureWidth,
                                    instance._directX11OverlayQuad.TextureHeight
                                );
                            }

                            instance._directX11OverlayQuad.Draw();
                        }
                    }
                }
                catch (Exception e)
                {
                    _presentHookDisabled = true;
                    Console.WriteLine("OMP.LSWTSS.Overlay1: overlay rendering disabled after error:");
                    Console.WriteLine(e);
                }
            }

            return _directX11SwapChainPresentMethodHook!.Trampoline!(swapChainNativeHandle, syncInterval, flags);
        }
    );
}
