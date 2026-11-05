namespace OMP.LSWTSS.CApi1;

public interface IClassVirtualMethodInfo : IClassMethodInfo
{
    public IVirtualClassInfo Class { get; }

    public uint NativeVtableIndex { get; }
}
