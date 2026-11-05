namespace OMP.LSWTSS.CApi1;

public static class GetClassMethodArgNativeDefSrc
{
    public static string Execute(ClassMethodArgSchema classMethodArgSchema)
    {
        var classMethodArgNativeDefSrc = $"{GetTypeNativeSrc.Execute(classMethodArgSchema.Type)} ";

        if (classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema)
        {
            if (classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string")
            {
                classMethodArgNativeDefSrc += $"{classMethodArgSchema.Name}NativeDataRawPtr";
            }
            else
            {
                classMethodArgNativeDefSrc += classMethodArgSchema.Name;
            }
        }
        else if (classMethodArgSchema.Type is ClassTypeSchema classMethodArgClassTypeSchema)
        {
            if (classMethodArgClassTypeSchema.IsClassNativeHandle)
            {
                classMethodArgNativeDefSrc += $"{classMethodArgSchema.Name}NativeDataRawPtr";
            }
            else
            {
                classMethodArgNativeDefSrc += $"{classMethodArgSchema.Name}RawPtr";
            }
        }
        else
        {
            throw new System.InvalidOperationException();
        }

        return classMethodArgNativeDefSrc;
    }
}