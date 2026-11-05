using System;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

partial class InputHook1
{
    readonly static unsafe CFuncHook1<User32Bindings.PeekMessageANativeDelegate> _user32PeekMessageAHook = new(
        User32Bindings.PeekMessageANativePtr,
        (
            lpMsg,
            hWnd,
            wMsgFilterMin,
            wMsgFilterMax,
            wRemoveMsg
        ) =>
        {
            var cursorOverrideImageNativeHandle = CursorOverrideImageNativeHandle;

            if (cursorOverrideImageNativeHandle != null)
            {
                var cursorInfo = new PInvoke.User32.CURSORINFO();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (PInvoke.User32.GetCursorInfo(&cursorInfo))
                {
                    if (cursorInfo.flags == PInvoke.User32.CURSORINFOFlags.CURSOR_HIDDEN)
                    {
                        _user32ShowCursorHook!.Trampoline!(true);
                    }
                }

                _user32SetCursorHook!.Trampoline!(cursorOverrideImageNativeHandle.Value);
            }

            bool ret;

            do
            {
                ret = _user32PeekMessageAHook!.Trampoline!(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);

                if (ret)
                {
                    if (
                        lpMsg->message >= PInvoke.User32.WindowMessage.WM_KEYFIRST
                        &&
                        lpMsg->message <= PInvoke.User32.WindowMessage.WM_KEYLAST
                    )
                    {
                        // Learn the game window hwnd from keyboard messages. We can't read
                        // it from the swap chain object (DXVK layout varies by build), but
                        // keyboard messages always carry the focused window's hwnd.
                        if (lpMsg->hwnd != nint.Zero)
                            _currentWindowNativeHandle = lpMsg->hwnd;

                        var nativeMessage = new NativeMessage(
                            lpMsg->hwnd,
                            (int)lpMsg->message,
                            lpMsg->wParam,
                            lpMsg->lParam,
                            lpMsg->pt.x,
                            lpMsg->pt.y
                        );

                        bool wasNativeMessageHandled = false;

                        lock (_clients)
                        {
                            foreach (var client in _clients)
                            {
                                if (client.HandleNativeMessage(nativeMessage))
                                {
                                    wasNativeMessageHandled = true;
                                    break;
                                }
                            }
                        }

                        if (wasNativeMessageHandled)
                        {
                            PInvoke.User32.TranslateMessage(lpMsg);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else if (lpMsg->hwnd == _currentWindowNativeHandle)
                    {
                        if (lpMsg->message == PInvoke.User32.WindowMessage.WM_INPUT)
                        {
                            var nativeMessage = new NativeMessage(
                                lpMsg->hwnd,
                                (int)lpMsg->message,
                                lpMsg->wParam,
                                lpMsg->lParam,
                                lpMsg->pt.x,
                                lpMsg->pt.y
                            );

                            bool wasNativeMessageHandled = false;

                            lock (_clients)
                            {
                                foreach (var client in _clients)
                                {
                                    if (client.HandleNativeMessage(nativeMessage))
                                    {
                                        wasNativeMessageHandled = true;
                                        break;
                                    }
                                }
                            }

                            if (wasNativeMessageHandled)
                            {
                                PInvoke.User32.TranslateMessage(lpMsg);
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if (
                            lpMsg->message >= PInvoke.User32.WindowMessage.WM_MOUSEFIRST
                            &&
                            lpMsg->message <= PInvoke.User32.WindowMessage.WM_MOUSELAST
                        )
                        {
                            if (
                                lpMsg->message == PInvoke.User32.WindowMessage.WM_MOUSEWHEEL
                                ||
                                lpMsg->message == PInvoke.User32.WindowMessage.WM_MOUSEHWHEEL
                            )
                            {
                                var nativeMessage = new NativeMessage(
                                    lpMsg->hwnd,
                                    (int)lpMsg->message,
                                    lpMsg->wParam,
                                    lpMsg->lParam,
                                    lpMsg->pt.x,
                                    lpMsg->pt.y
                                );

                                bool wasNativeMessageHandled = false;

                                lock (_clients)
                                {
                                    foreach (var client in _clients)
                                    {
                                        if (client.HandleNativeMessage(nativeMessage))
                                        {
                                            wasNativeMessageHandled = true;
                                            break;
                                        }
                                    }
                                }

                                if (wasNativeMessageHandled)
                                {
                                    PInvoke.User32.TranslateMessage(lpMsg);
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                var x = (short)lpMsg->lParam;
                                var y = (short)(lpMsg->lParam >> 16);

                                PInvoke.User32.GetClientRect(lpMsg->hwnd, out var clientRect);

                                if (x >= 0 && x <= clientRect.right - clientRect.left && y >= 0 && y <= clientRect.bottom - clientRect.top)
                                {
                                    var nativeMessage = new NativeMessage(
                                        lpMsg->hwnd,
                                        (int)lpMsg->message,
                                        lpMsg->wParam,
                                        lpMsg->lParam,
                                        lpMsg->pt.x,
                                        lpMsg->pt.y
                                    );

                                    bool wasNativeMessageHandled = false;

                                    lock (_clients)
                                    {
                                        foreach (var client in _clients)
                                        {
                                            if (client.HandleNativeMessage(nativeMessage))
                                            {
                                                wasNativeMessageHandled = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (wasNativeMessageHandled)
                                    {
                                        PInvoke.User32.TranslateMessage(lpMsg);
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

            } while (ret);

            return false;
        }
    );
}