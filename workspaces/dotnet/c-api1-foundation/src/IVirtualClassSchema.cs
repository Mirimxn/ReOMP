namespace OMP.LSWTSS.CApi1;

public interface IVirtualClassSchema : IClassSchema
{
    public uint? NativeVtableSteamRuntimeOffset { get; set; }

    public uint? NativeVtableEGSRuntimeOffset { get; set; }
}