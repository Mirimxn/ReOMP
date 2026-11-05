using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public unsafe sealed class ClassRegisteredPropertyElementsGenericValueNativeDataAccessor<TPropertyElementValueNativeData>(nint nativeDataRawPtr, ApiClassField.NativeHandle propertyApiClassField) : IEnumerable<TPropertyElementValueNativeData> where TPropertyElementValueNativeData : struct
{
    private readonly nint _nativeDataRawPtr = nativeDataRawPtr;

    private readonly ApiClassField.NativeHandle _propertyApiClassField = propertyApiClassField;

    public int Length => _propertyApiClassField.GetMemberCount(_nativeDataRawPtr);

    public TPropertyElementValueNativeData this[uint propertyElementIndex]
    {
        get
        {
            var propertyElementValueNativeData = default(TPropertyElementValueNativeData);

            _propertyApiClassField.GetMemberDataByIndex(
                _nativeDataRawPtr,
                0,
                propertyElementIndex,
                (nint)(&propertyElementValueNativeData),
                sizeof(TPropertyElementValueNativeData),
                0,
                0
            );

            return propertyElementValueNativeData;
        }
        set
        {
            _propertyApiClassField.SetMemberDataByIndex(
                _nativeDataRawPtr,
                0,
                propertyElementIndex,
                (nint)(&value),
                sizeof(TPropertyElementValueNativeData),
                0,
                0
            );
        }
    }
    
    public TPropertyElementValueNativeData[] ToArray()
    {
        var arrayLength = Length;

        var array = new TPropertyElementValueNativeData[arrayLength];

        for (uint i = 0; i < arrayLength; i++)
        {
            array[i] = this[i];
        }

        return array;
    }

    public IEnumerator<TPropertyElementValueNativeData> GetEnumerator()
    {
        return ToArray().AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}