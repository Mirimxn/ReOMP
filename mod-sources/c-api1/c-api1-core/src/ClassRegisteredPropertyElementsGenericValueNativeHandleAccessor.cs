using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public unsafe sealed class ClassRegisteredPropertyElementsGenericValueNativeHandleAccessor<TPropertyElementValueNativeHandle>(nint nativeDataRawPtr, ApiClass.NativeHandle apiClass, ApiClassField.NativeHandle propertyApiClassField) : IEnumerable<TPropertyElementValueNativeHandle> where TPropertyElementValueNativeHandle : struct
{
    private readonly nint _nativeDataRawPtr = nativeDataRawPtr;

    private readonly ApiClass.NativeHandle _apiClass = apiClass;

    private readonly ApiClassField.NativeHandle _propertyApiClassField = propertyApiClassField;

    public int Length => _propertyApiClassField.GetMemberCount(_nativeDataRawPtr);

    public TPropertyElementValueNativeHandle this[uint propertyElementIndex]
    {
        get
        {
            var classObject = new ClassObject.NativeData
            {
                SelfApiClass = _apiClass,
                SelfNativeDataRawPtr = _nativeDataRawPtr
            };

            var propertyElementValueClassObject = _propertyApiClassField.GetMemberObjectByIndex(
                (ClassObject.NativeHandle)(&classObject),
                propertyElementIndex
            );

            return System.Runtime.CompilerServices.Unsafe.As<nint, TPropertyElementValueNativeHandle>(ref propertyElementValueClassObject.SelfNativeDataRawPtr);
        }
    }

    public TPropertyElementValueNativeHandle[] ToArray()
    {
        var arrayLength = Length;

        var array = new TPropertyElementValueNativeHandle[arrayLength];

        for (uint i = 0; i < arrayLength; i++)
        {
            array[i] = this[i];
        }

        return array;
    }

    public IEnumerator<TPropertyElementValueNativeHandle> GetEnumerator()
    {
        return ToArray().AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}