namespace OMP.LSWTSS.CApi1;

public sealed class ClassRegisteredPropertySchema
{
    public required string ApiClassFieldName { get; set; }

    public required string Name { get; set; }

    public required ITypeSchema Type { get; set; }

    public required bool IsArray { get; set; }
}