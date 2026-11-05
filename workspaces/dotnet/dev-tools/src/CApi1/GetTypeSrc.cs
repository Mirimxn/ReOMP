namespace OMP.LSWTSS.CApi1;

public static class GetTypeSrc
{
    public static string Execute(ITypeSchema typeSchema)
    {
        if (typeSchema is PrimitiveTypeSchema primitiveTypeSchema)
        {
            var primitiveTypeSchemaSrc = primitiveTypeSchema.PrimitiveFullName;

            if (primitiveTypeSchema.PrimitiveFullName == "string")
            {
                primitiveTypeSchemaSrc += "?";
            }

            if (primitiveTypeSchema.IsPrimitiveNativeDataPtr)
            {
                primitiveTypeSchemaSrc += "*";
            }

            return primitiveTypeSchemaSrc;
        }
        else if (typeSchema is ClassTypeSchema classTypeSchema)
        {
            var classTypeSchemaSrc = classTypeSchema.ClassFullName;

            if (classTypeSchema.IsClassNativeHandle)
            {
                classTypeSchemaSrc += ".NativeHandle";
            }
            else
            {
                classTypeSchemaSrc += ".NativeData";
            }

            if (classTypeSchema.ClassGenerics != null)
            {
                classTypeSchemaSrc += classTypeSchema.ClassGenerics;
            }

            return classTypeSchemaSrc;
        }
        else
        {
            throw new System.InvalidOperationException();
        }
    }
}