using System;
using System.IO;

namespace OMP.LSWTSS;

partial class Overlay1
{
    static Overlay1()
    {
        var cefOffScreenSettings = new CefSharp.OffScreen.CefSettings
        {
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "omp-lswtss-overlay1\\Cache")
        };

        // This line improves the performance
        cefOffScreenSettings.CefCommandLineArgs.Add("disable-threaded-scrolling", "1");
        // Wine/Proton: prevent Chromium from querying WinRT UISettings/UIViewSettings (no-op on Windows)
        cefOffScreenSettings.CefCommandLineArgs.Add("force-color-profile", "srgb");
        cefOffScreenSettings.CefCommandLineArgs.Add("disable-features", "CalculateNativeWinOcclusion,WinStylusInput");
        cefOffScreenSettings.CefCommandLineArgs.Add("disable-direct-composition", "1");

        CefSharp.Cef.Initialize(
            cefOffScreenSettings,
            performDependencyCheck: true,
            browserProcessHandler: null
        );

        _directX11SwapChainPresentMethodHook.Enable();
        _directX11SwapChainResizeBuffersMethodHook.Enable();
    }
}