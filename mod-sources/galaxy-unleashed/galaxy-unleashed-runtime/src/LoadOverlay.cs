using System;
using System.Diagnostics;
using CefSharp;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed : IDisposable
{
    class OverlayChromieumWebBrowserLifeSpanHandler : ILifeSpanHandler
    {
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            return false;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser? newBrowser)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = "\"" + targetUrl + "\""
                });
            }
            catch
            {
            }

            newBrowser = null;
            return true;
        }
    }

    void LoadOverlay()
    {
        _overlay.ChromiumWebBrowser.JavascriptObjectRepository.NameConverter =
            new CefSharp.JavascriptBinding.CamelCaseJavascriptNameConverter();

        _overlay.ChromiumWebBrowser.JavascriptObjectRepository.Register(
            "galaxyUnleashedRuntimeApi",
            new RuntimeApi(),
            options: CefSharp.BindingOptions.DefaultBinder
        );

        _overlay.ChromiumWebBrowser.LifeSpanHandler = new OverlayChromieumWebBrowserLifeSpanHandler();

        string overlayUrl;

        if (Environment.GetEnvironmentVariable("DEV_GALAXY_UNLEASHED") == "1")
        {
            overlayUrl = "http://localhost:5173/";
        }
        else
        {
            string overlayIndexHtmlFilePath = System.IO.Path.Combine(
                System.IO.Path.Combine(Environment.CurrentDirectory, "mods", "galaxy-unleashed"),
                "index.html"
            ).Replace("\\", "/");

            overlayUrl = "file://" + overlayIndexHtmlFilePath;
        }

        _overlay.ChromiumWebBrowser.LoadUrl(overlayUrl);
    }
}