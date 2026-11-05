namespace OMP.LSWTSS.CApi1;

public interface IVirtualClassInfo : IClassInfo
{
    public nint NativeVtablePtr { get; }
}
