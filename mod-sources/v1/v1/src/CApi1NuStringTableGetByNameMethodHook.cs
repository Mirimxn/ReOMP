using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Custom localisation strings: string id -> UTF-8 text (allocated once, never freed).
    static readonly Dictionary<string, nint> _customStrings = new(StringComparer.OrdinalIgnoreCase);

    static int _customStringHitsLogged = 0;

    static void RegisterCustomString(string stringId, string text)
    {
        if (_customStrings.TryGetValue(stringId, out var old)) Marshal.FreeCoTaskMem(old);
        _customStrings[stringId] = Marshal.StringToCoTaskMemUTF8(text);
    }

    static readonly unsafe CFuncHook1<NuStringTable.GetByNameMethod.NativeDelegate> _cApi1NuStringTableGetByNameMethodHook = new(
        NuStringTable.GetByNameMethod.Info.NativePtr,
        (arg0, arg1) =>
        {
            if (_customStrings.Count > 0 && arg0 != nint.Zero)
            {
                try
                {
                    var stringId = Marshal.PtrToStringUTF8(arg0);
                    if (stringId != null && _customStrings.TryGetValue(stringId, out var text))
                    {
                        if (_customStringHitsLogged < 10)
                        {
                            _customStringHitsLogged++;
                            VehicleDebug($"string lookup '{stringId}' -> custom text");
                        }
                        return text;
                    }
                }
                catch
                {
                }
            }

            return _cApi1NuStringTableGetByNameMethodHook!.Trampoline!(arg0, arg1);
        }
    );
}
