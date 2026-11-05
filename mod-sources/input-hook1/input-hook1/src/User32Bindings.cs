namespace OMP.LSWTSS;

partial class InputHook1
{
    static unsafe class User32Bindings
    {
        public delegate int ShowCursorNativeDelegate(bool bShow);

        public delegate int SetCursorNativeDelegate(nint hCursor);

        public delegate bool PeekMessageANativeDelegate(PInvoke.User32.MSG* lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        public static readonly nint ShowCursorNativePtr;

        public static readonly nint SetCursorNativePtr;

        public static readonly nint PeekMessageANativePtr;

        static User32Bindings()
        {
            using var user32ModuleNativeHandle = PInvoke.Kernel32.LoadLibrary("user32.dll");

            ShowCursorNativePtr = PInvoke.Kernel32.GetProcAddress(user32ModuleNativeHandle, "ShowCursor");

            SetCursorNativePtr = PInvoke.Kernel32.GetProcAddress(user32ModuleNativeHandle, "SetCursor");

            PeekMessageANativePtr = PInvoke.Kernel32.GetProcAddress(user32ModuleNativeHandle, "PeekMessageA");
        }
    }
}