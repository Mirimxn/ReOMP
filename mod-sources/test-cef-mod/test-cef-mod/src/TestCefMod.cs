using System;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod : IDisposable
{
    readonly Overlay1 _overlay;

    readonly InputHook1.Client _inputHookClient;

    readonly CFuncHook1<GameFramework.ProcessMethod.NativeDelegate> _gameFrameworkProcessMethodHook;

    bool _isDisposed;

    public TestCefMod()
    {
        _isDisposed = false;

        _overlay = new Overlay1(1);

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
            (
                in InputHook1.NativeMessage inputHookClientNativeMessage
            ) =>
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYDOWN)
                {
                    if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F1)
                    {
                        if (IsAnyMenuOpened)
                        {
                            _quickMenuState = null;
                            _spawnerMenuState = null;
                        }
                        else if (_spawnerInPlayerEntityRange != null)
                        {
                            _spawnerMenuState = new SpawnerMenuState
                            {
                                SpawnerId = _spawnerInPlayerEntityRange.Id
                            };
                        }
                        else
                        {
                            _quickMenuState = new QuickMenuState
                            {
                            };
                        }

                        UpdateOverlay();

                        return true;
                    }
                }

                UpdateOverlay();

                return false;
            }
        );

        _gameFrameworkProcessMethodHook = new(
            GameFramework.ProcessMethod.Info.NativePtr,
            (nativeDataRawPtr) =>
            {
                try
                {
                    OnUpdate();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return _gameFrameworkProcessMethodHook!.Trampoline!(nativeDataRawPtr);
            }
        );

        _gameFrameworkProcessMethodHook.Enable();
    }

    void LoadOverlay()
    {
        _overlay.ChromiumWebBrowser.JavascriptObjectRepository.NameConverter =
            new CefSharp.JavascriptBinding.CamelCaseJavascriptNameConverter();

        _overlay.ChromiumWebBrowser.JavascriptObjectRepository.Register(
            "testCefMod",
            new JavaScriptObject(),
            options: CefSharp.BindingOptions.DefaultBinder
        );

        string overlayIndexHtmlFilePath = System.IO.Path.Combine(
            System.IO.Path.Combine(Environment.CurrentDirectory, "mods", "test-cef-mod"),
            "index.html"
        ).Replace("\\", "/");

        _overlay.ChromiumWebBrowser.LoadUrl("file://" + overlayIndexHtmlFilePath);
    }

    void UpdateOverlay()
    {
        _overlay.AreMouseEventsEnabled = false;
        _overlay.IsActive = IsAnyMenuOpened;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _gameFrameworkProcessMethodHook.Dispose();

        _inputHookClient.Dispose();

        _overlay.Dispose();

        foreach (var spawner in _spawners.ToList())
        {
            spawner.Dispose();
        }

        _spawners.Clear();

        foreach (var spawnNpcTask in _spawnNpcTasks.ToList())
        {
            spawnNpcTask.Dispose();
        }

        _spawnNpcTasks.Clear();

        foreach (var npc in _npcs.ToList())
        {
            npc.Dispose();
        }

        _npcs.Clear();

        _playerEntityLastPosition = null;
        _spawnerInPlayerEntityRange = null;

        _isDisposed = true;
    }

    ~TestCefMod()
    {
        Dispose();
    }
}