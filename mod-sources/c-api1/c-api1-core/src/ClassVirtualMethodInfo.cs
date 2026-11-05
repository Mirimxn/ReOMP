namespace OMP.LSWTSS.CApi1;

public sealed class ClassVirtualMethodInfo(IVirtualClassInfo class_, uint nativeVtableIndex, bool isStatic) : IClassVirtualMethodInfo
{
    public IVirtualClassInfo Class { get; private set; } = class_;

    public uint NativeVtableIndex { get; private set; } = nativeVtableIndex;

    public nint NativePtr { get; private set; } = Native.GetVtableMethodPtr(class_.NativeVtablePtr, nativeVtableIndex);

    public bool IsStatic { get; private set; } = isStatic;
}
