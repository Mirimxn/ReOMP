using System;
using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public static unsafe partial class ApiClass
{
    public static readonly List<NativeHandle> Instances = [];

    public partial struct NativeHandle
    {
        public readonly ApiClassField.NativeHandle[] GetFields()
        {
            var fields = new List<ApiClassField.NativeHandle>();

            if (NativeDataPtr->FirstField != nint.Zero)
            {
                var currentIterField = NativeDataPtr->FirstField;

                do
                {
                    fields.Add(currentIterField);

                    currentIterField = currentIterField.NativeDataPtr->Next;
                } while (currentIterField != nint.Zero);
            }

            return [.. fields];
        }

        public readonly ApiFunction.NativeHandle[] GetFunctions()
        {
            return NativeDataPtr->FunctionsVectorNativeData.GetElements();
        }
    }
}