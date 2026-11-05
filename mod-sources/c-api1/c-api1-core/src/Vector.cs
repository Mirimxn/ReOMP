using System.Runtime.InteropServices;

namespace OMP.LSWTSS.CApi1;

public unsafe struct Vector
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeData<TElement>
    {
        public static readonly int NativeDataSize = Marshal.SizeOf<NativeData<TElement>>();

        public TElement* ElementsBeginIteratorPtr;

        public TElement* ElementsEndIteratorPtr;

        public TElement* ElementsCapacityAllocatorPtr;

        public readonly TElement GetElementAtIndex(uint elementIndex)
        {
            return ((TElement*)(((nint)ElementsBeginIteratorPtr) + elementIndex * Marshal.SizeOf<TElement>()))[0];
        }

        public readonly nint GetElementsCount()
        {
            if ((nint)ElementsBeginIteratorPtr == 0 || (nint)ElementsEndIteratorPtr == 0)
            {
                return 0;
            }

            return ((nint)ElementsEndIteratorPtr - (nint)ElementsBeginIteratorPtr) / Marshal.SizeOf<TElement>();
        }

        public readonly TElement[] GetElements()
        {
            var elementsCount = GetElementsCount();

            var elements = new TElement[elementsCount];

            for (var i = 0; i < elementsCount; i++)
            {
                elements[i] = GetElementAtIndex((uint)i);
            }

            return elements;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeHandle<TElement>
    {
        public NativeData<TElement>* NativeDataPtr;

        public readonly nint NativeDataRawPtr => (nint)NativeDataPtr;

        public static implicit operator nint(NativeHandle<TElement> @this) => @this.NativeDataRawPtr;

        public static implicit operator NativeData<TElement>*(NativeHandle<TElement> @this) => @this.NativeDataPtr;

        public static implicit operator NativeHandle<TElement>(NativeData<TElement>* nativeDataPtr) => new() { NativeDataPtr = nativeDataPtr };

        public static explicit operator NativeHandle<TElement>(nint nativeDataRawPtr) => new() { NativeDataPtr = (NativeData<TElement>*)nativeDataRawPtr };
    }
}