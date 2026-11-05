namespace OMP.LSWTSS;

partial class InputHook1
{
    readonly static CFuncHook1<User32Bindings.SetCursorNativeDelegate> _user32SetCursorHook = new(
        User32Bindings.SetCursorNativePtr,
        (
            hCursor
        ) =>
        {
            if (CursorOverrideImageNativeHandle != null)
            {
                return 0;
            }

            return _user32SetCursorHook!.Trampoline!(hCursor);
        }
    );
}