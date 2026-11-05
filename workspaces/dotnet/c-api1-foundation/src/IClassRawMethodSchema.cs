namespace OMP.LSWTSS.CApi1;

public interface IClassRawMethodSchema : IClassMethodSchema
{
    public uint? NativeSteamRuntimeOffset { get; set; }

    public uint? NativeEGSRuntimeOffset { get; set; }
}