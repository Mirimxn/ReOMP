namespace OMP.LSWTSS;

partial class InputHook1
{
    readonly static CFuncHook1<User32Bindings.ShowCursorNativeDelegate> _user32ShowCursorHook = new(
        User32Bindings.ShowCursorNativePtr,
        (
            bShow
        ) =>
        {
            if (CursorOverrideImageNativeHandle != null)
            {
                return bShow ? 1 : 0;
            }

            return _user32ShowCursorHook!.Trampoline!(bShow);
        }
    );
}