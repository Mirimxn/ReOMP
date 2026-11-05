using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public unsafe sealed class ClassRegisteredPropertyElementsStringValueNativeDataAccessor(nint nativeDataRawPtr, ApiClassField.NativeHandle propertyApiClassField) : IEnumerable<string?>
{
    private readonly nint _nativeDataRawPtr = nativeDataRawPtr;

    private readonly ApiClassField.NativeHandle _propertyApiClassField = propertyApiClassField;

    public int Length => _propertyApiClassField.GetMemberCount(_nativeDataRawPtr);

    public string? this[uint propertyElementIndex]
    {
        get
        {
            var propertyElementValueNativeDataRawPtr = Marshal.AllocCoTaskMem(_propertyApiClassField.NativeDataPtr->StringMaxLength);

            _propertyApiClassField.GetMemberDataByIndex(
                _nativeDataRawPtr,
                0,
                propertyElementIndex,
                propertyElementValueNativeDataRawPtr,
                _propertyApiClassField.NativeDataPtr->StringMaxLength,
                0,
                0
            );

            var propertyElementValueNativeData = Marshal.PtrToStringUTF8(propertyElementValueNativeDataRawPtr);

            Marshal.FreeCoTaskMem(propertyElementValueNativeDataRawPtr);

            return propertyElementValueNativeData;
        }
        set
        {
            var propertyElementValueNativeDataRawPtr = Marshal.StringToCoTaskMemUTF8(value);

            _propertyApiClassField.SetMemberDataByIndex(
                _nativeDataRawPtr,
                0,
                propertyElementIndex,
                propertyElementValueNativeDataRawPtr,
                value == null ? 0 : value.Length + 1,
                0,
                0
            );

            Marshal.FreeCoTaskMem(propertyElementValueNativeDataRawPtr);
        }
    }

    public string?[] ToArray()
    {
        var arrayLength = Length;

        var array = new string?[arrayLength];

        for (uint i = 0; i < arrayLength; i++)
        {
            array[i] = this[i];
        }

        return array;
    }

    public IEnumerator<string?> GetEnumerator()
    {
        return ToArray().AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}