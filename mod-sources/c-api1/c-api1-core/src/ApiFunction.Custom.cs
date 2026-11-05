using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public static unsafe partial class ApiFunction
{
    public partial struct NativeHandle
    {
        public readonly string? GetName()
        {
            return Marshal.PtrToStringUTF8(NativeDataPtr->NameNativeDataRawPtr);
        }

        public readonly bool GetIsMethod()
        {
            return NativeDataPtr->IsMethodFlag != 0;
        }

        public readonly bool GetIsConst()
        {
            return NativeDataPtr->IsConstFlag != 0;
        }

        public uint? GetNativeDataRawPtrOffset()
        {
            if (NativeDataPtr->TargetObjectOffsetIfApiMemberFn > 0 && NativeDataPtr->TargetObjectOffsetIfApiMemberFn < 4096)
            {
                return (uint)NativeDataPtr->TargetObjectOffsetIfApiMemberFn;
            }

            return null;
        }
    }
}