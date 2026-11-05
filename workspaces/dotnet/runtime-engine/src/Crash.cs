using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static Exception Crash(string message)
    {
        PInvoke.User32.MessageBox(
            nint.Zero,
            message,
            "Crash!",
            PInvoke.User32.MessageBoxOptions.MB_ICONERROR
            |
            PInvoke.User32.MessageBoxOptions.MB_OK
        );

        Environment.Exit(1);

        throw new Exception();
    }
}