using System;

namespace OMP.LSWTSS;

partial class Overlay1 : IDisposable
{
    bool _isDisposed;

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _instances.Remove(this);

            SortInstances();

            _inputHookClient.Dispose();
            ChromiumWebBrowser.RenderHandler.Dispose();
            ChromiumWebBrowser.Dispose();

            _isDisposed = true;
        }
    }
}