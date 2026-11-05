using System;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public static unsafe partial class ApiFunctionPrototype
{
    public partial struct NativeHandle
    {
        public readonly bool GetIsReturnTypePtr()
        {
            return NativeDataPtr->IsReturnTypePtrFlag != 0;
        }

        public readonly bool GetIsReturnTypeRef()
        {
            return NativeDataPtr->IsReturnTypeRefFlag != 0;
        }

        public readonly bool GetIsReturnTypeConst()
        {
            return NativeDataPtr->IsReturnTypeConstFlag != 0;
        }

        public readonly ApiType.NativeHandle[] GetArgTypes()
        {
            return NativeDataPtr->ArgTypesVectorNativeData.GetElements();
        }

        public readonly byte GetArgTypeFlags(byte argTypeIndex)
        {
            return argTypeIndex switch
            {
                0 => NativeDataPtr->ArgType0Flags,
                1 => NativeDataPtr->ArgType1Flags,
                2 => NativeDataPtr->ArgType2Flags,
                3 => NativeDataPtr->ArgType3Flags,
                4 => NativeDataPtr->ArgType4Flags,
                5 => NativeDataPtr->ArgType5Flags,
                6 => NativeDataPtr->ArgType6Flags,
                7 => NativeDataPtr->ArgType7Flags,
                8 => NativeDataPtr->ArgType8Flags,
                9 => NativeDataPtr->ArgType9Flags,
                10 => NativeDataPtr->ArgType10Flags,
                11 => NativeDataPtr->ArgType11Flags,
                _ => throw new System.InvalidOperationException()
            };
        }

        public readonly bool GetIsArgTypePtr(byte argTypeIndex)
        {
            return (GetArgTypeFlags(argTypeIndex) & 0x1) != 0;
        }

        public readonly bool GetIsArgTypeRef(byte argTypeIndex)
        {
            return (GetArgTypeFlags(argTypeIndex) & 0x2) != 0;
        }

        public readonly bool GetIsArgTypeConst(byte argTypeIndex)
        {
            return (GetArgTypeFlags(argTypeIndex) & 0x4) != 0;
        }

        public readonly string? GetDefString()
        {
            return Marshal.PtrToStringUTF8(NativeDataPtr->DefStringNativeDataRawPtr);
        }
    }
}