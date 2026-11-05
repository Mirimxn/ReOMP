namespace OMP.LSWTSS.CApi1;

public interface IClassMethodInfo
{
    public nint NativePtr { get; }

    public bool IsStatic { get; }
}
