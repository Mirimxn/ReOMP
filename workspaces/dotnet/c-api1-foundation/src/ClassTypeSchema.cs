namespace OMP.LSWTSS.CApi1;

public sealed class ClassTypeSchema : ITypeSchema
{
    public required string ClassFullName { get; set; }

    public required string? ClassGenerics { get; set; }

    public required bool IsClassNativeHandle { get; set; }
}