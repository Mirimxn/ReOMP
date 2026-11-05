using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public static unsafe partial class ApiClassField
{
    public partial struct NativeHandle
    {
        public readonly string? GetName()
        {
            return Marshal.PtrToStringUTF8(NativeDataPtr->NameNativeDataRawPtr);
        }

        public readonly string? GetTypeName()
        {
            return Marshal.PtrToStringUTF8(NativeDataPtr->TypeNameNativeDataRawPtr);
        }

        public readonly bool GetIsArray()
        {
            return NativeDataPtr->ArraySizeIfNotMinusOne > -1;
        }
    }
}