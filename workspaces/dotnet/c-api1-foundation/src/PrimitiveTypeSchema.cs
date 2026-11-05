namespace OMP.LSWTSS.CApi1;

public sealed class PrimitiveTypeSchema : ITypeSchema
{
    public required string PrimitiveFullName { get; set; }

    public required bool IsPrimitiveNativeDataPtr { get; set; }
}