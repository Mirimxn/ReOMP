namespace OMP.LSWTSS.CApi1;

public static class GetClassMethodArgCallSrc
{
    public static string Execute(ClassMethodArgSchema classMethodArgSchema)
    {
        if (classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema)
        {
            if (classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string")
            {
                return $"{classMethodArgSchema.Name}NativeDataRawPtr";
            }

            return classMethodArgSchema.Name;
        }
        else if (classMethodArgSchema.Type is ClassTypeSchema classMethodArgClassTypeSchema)
        {
            if (classMethodArgClassTypeSchema.IsClassNativeHandle)
            {
                return $"{classMethodArgSchema.Name}.NativeDataRawPtr";
            }

            return $"(nint)(&{classMethodArgSchema.Name})";
        }
        else
        {
            throw new System.InvalidOperationException();
        }
    }
}