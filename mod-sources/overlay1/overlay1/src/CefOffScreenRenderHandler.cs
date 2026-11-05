using System;

namespace OMP.LSWTSS;

partial class Overlay1
{
    sealed class CefOffScreenRenderHandler : CefSharp.OffScreen.IRenderHandler
    {
        readonly Overlay1 _overlay1;

        bool _isDisposed;

        public CefOffScreenRenderHandler(Overlay1 overlay1)
        {
            _overlay1 = overlay1;
        }

        public CefSharp.Structs.ScreenInfo? GetScreenInfo()
        {
            return null;
        }

        public bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        public CefSharp.Structs.Rect GetViewRect()
        {
            if (_overlay1._directX11OverlayQuad != null)
            {
                return new CefSharp.Structs.Rect(
                    0,
                    0,
                    _overlay1._directX11OverlayQuad.TextureWidth,
                    _overlay1._directX11OverlayQuad.TextureHeight
                );
            }

            return new CefSharp.Structs.Rect(0, 0, 800, 600);
        }

        public void OnAcceleratedPaint(CefSharp.PaintElementType type, CefSharp.Structs.Rect dirtyRect, CefSharp.AcceleratedPaintInfo acceleratedPaintInfo)
        {
            // Not used: the browser is created with SharedTextureEnabled = false,
            // so CEF delivers frames via OnPaint (software) instead.
        }

        public void OnCursorChange(nint cursor, CefSharp.Enums.CursorType type, CefSharp.Structs.CursorInfo customCursorInfo)
        {
            _overlay1._cursorLastImageNativeHandle = cursor;
        }

        public void OnImeCompositionRangeChanged(CefSharp.Structs.Range selectedRange, CefSharp.Structs.Rect[] characterBounds)
        {
        }

        public void OnPaint(CefSharp.PaintElementType type, CefSharp.Structs.Rect dirtyRect, nint buffer, int width, int height)
        {
            if (
                type == CefSharp.PaintElementType.View
                &&
                _overlay1._directX11OverlayQuad != null
            )
            {
                _overlay1._directX11OverlayQuad.QueueBgraTextureUpdate(buffer, width, height);
            }
        }

        public void OnPopupShow(bool show)
        {
        }

        public void OnPopupSize(CefSharp.Structs.Rect rect)
        {
        }

        public void OnVirtualKeyboardRequested(CefSharp.IBrowser browser, CefSharp.Enums.TextInputMode inputMode)
        {
        }

        public bool StartDragging(CefSharp.IDragData dragData, CefSharp.Enums.DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        public void UpdateDragCursor(CefSharp.Enums.DragOperationsMask operation)
        {
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }
    }
}