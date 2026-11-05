namespace OMP.LSWTSS;

partial class InputHook1
{
    static InputHook1()
    {
        _directX11SwapChainPresentMethodHook.Enable();
        _user32SetCursorHook.Enable();
        _user32ShowCursorHook.Enable();
        _user32PeekMessageAHook.Enable();
    }
}