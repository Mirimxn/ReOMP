namespace OMP.LSWTSS.CApi1;

public interface IClassVirtualMethodSchema : IClassMethodSchema
{
    public uint NativeVtableIndex { get; set; }
}