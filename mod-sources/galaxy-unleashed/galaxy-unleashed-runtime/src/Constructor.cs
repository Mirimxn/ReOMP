using System;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    bool _isDisposed;

    readonly Overlay1 _overlay;

    readonly InputHook1.Client _inputHookClient;

    readonly CFuncHook1<GameFramework.ProcessMethod.NativeDelegate> _gameFrameworkProcessMethodHook;

    static GalaxyUnleashed? _instance;

    public GalaxyUnleashed()
    {
        _isDisposed = false;

        _overlay = new Overlay1(order: 1)
        {
            AreKeyboardEventsEnabled = true,
            AreMouseEventsEnabled = true,
        };

        if (_overlay.ChromiumWebBrowser.IsBrowserInitialized)
        {
            LoadOverlay();
        }
        else
        {
            _overlay.ChromiumWebBrowser.BrowserInitialized += (sender, e) =>
            {
                LoadOverlay();
            };
        }

        _inputHookClient = new InputHook1.Client(
            0,
            InputHookClientHandler
        );

        _gameFrameworkProcessMethodHook = new(
            GameFramework.ProcessMethod.Info.NativePtr,
            (nativeDataPtr) =>
            {
                try
                {
                    _instance?.Update();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return _gameFrameworkProcessMethodHook!.Trampoline!(nativeDataPtr);
            }
        );

        _gameFrameworkProcessMethodHook.Enable();

        _instance = this;
    }
}
