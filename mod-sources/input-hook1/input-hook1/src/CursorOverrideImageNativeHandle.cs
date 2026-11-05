using System.Linq;

namespace OMP.LSWTSS;

partial class InputHook1
{
    static nint? CursorOverrideImageNativeHandle
    {
        get
        {
            lock (_clients)
            {
                return _clients
                    .FirstOrDefault((client) => client.CursorOverrideImageNativeHandle.HasValue)?
                    .CursorOverrideImageNativeHandle;
            }
        }
    }
}