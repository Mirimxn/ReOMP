using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

partial class Overlay1
{
    static class DirectX11Bindings
    {
        public delegate int SwapChainPresentMethodNativeDelegate(nint swapChainNativeHandle, int syncInterval, int flags);

        public delegate int SwapChainResizeBuffersMethodNativeDelegate(nint swapChainNativeHandle, int bufferCount, int width, int height, int newFormat, int swapChainFlags);

        public static readonly nint SwapChainPresentMethodNativePtr;

        public static readonly nint SwapChainResizeBuffersMethodNativePtr;

        static DirectX11Bindings()
        {
            var windowNativeHandle = PInvoke.User32.CreateWindowEx(
                0,
                "STATIC",
                "ID3D11DeviceWnd",
                PInvoke.User32.WindowStyles.WS_OVERLAPPEDWINDOW,
                -2147483648, // CW_USEDEFAULT
                -2147483648, // CW_USEDEFAULT
                640,
                480,
                nint.Zero,
                nint.Zero,
                nint.Zero,
                nint.Zero
            );

            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.None,
                new SharpDX.DXGI.SwapChainDescription
                {
                    ModeDescription = new SharpDX.DXGI.ModeDescription
                    {
                        Width = 640,
                        Height = 480,
                        RefreshRate = new SharpDX.DXGI.Rational(60, 1),
                        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb,
                        ScanlineOrdering = SharpDX.DXGI.DisplayModeScanlineOrder.Unspecified,
                        Scaling = SharpDX.DXGI.DisplayModeScaling.Centered,
                    },
                    SampleDescription = new SharpDX.DXGI.SampleDescription
                    {
                        Count = 1,
                        Quality = 0,
                    },
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    BufferCount = 2,
                    OutputHandle = windowNativeHandle,
                    IsWindowed = true,
                    SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                    Flags = 0,
                },
                out var device,
                out var swapChain
            );

            var swapChainNativeVtablePtr = Marshal.ReadIntPtr(swapChain.NativePointer);

            using (device)
            {
                using (swapChain)
                {
                    SwapChainPresentMethodNativePtr = Marshal.ReadIntPtr(swapChainNativeVtablePtr, 8 * nint.Size);
                    SwapChainResizeBuffersMethodNativePtr = Marshal.ReadIntPtr(swapChainNativeVtablePtr, 13 * nint.Size);
                }
            }

            PInvoke.User32.DestroyWindow(windowNativeHandle);
        }
    }
}