namespace OMP.LSWTSS.CApi1;

public sealed class ClassFieldSchema
{
    public required string Name { get; set; }

    public required ITypeSchema Type { get; set; }

    public required string? Comment { get; set; }
}