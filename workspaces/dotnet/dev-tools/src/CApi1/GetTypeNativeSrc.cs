namespace OMP.LSWTSS.CApi1;

public static class GetTypeNativeSrc
{
    public static string Execute(ITypeSchema typeSchema)
    {
        if (typeSchema is PrimitiveTypeSchema primitiveTypeSchema)
        {
            if (primitiveTypeSchema.PrimitiveFullName == "string")
            {
                return "nint";
            }

            return GetTypeSrc.Execute(primitiveTypeSchema);
        }
        else if (typeSchema is ClassTypeSchema classTypeSchema)
        {
            return "nint";
        }
        else
        {
            throw new System.InvalidOperationException();
        }
    }
}