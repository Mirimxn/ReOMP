namespace OMP.LSWTSS;

partial class InputHook1
{
    public readonly struct NativeMessage(nint windowHandle, int type, nint wParam, nint lParam, int mouseScreenX, int mouseScreenY)
    {
        public readonly nint WindowHandle = windowHandle;

        public readonly int Type = type;

        public readonly nint WParam = wParam;

        public readonly nint LParam = lParam;

        public readonly int MouseScreenX = mouseScreenX;

        public readonly int MouseScreenY = mouseScreenY;
    }
}