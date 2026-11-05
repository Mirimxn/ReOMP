namespace OMP.LSWTSS.CApi1;

public static class GetClassMethodArgDefSrc
{
    public static string Execute(ClassMethodArgSchema classMethodArgSchema)
    {
        return $"{GetTypeSrc.Execute(classMethodArgSchema.Type)} {classMethodArgSchema.Name}";
    }
}