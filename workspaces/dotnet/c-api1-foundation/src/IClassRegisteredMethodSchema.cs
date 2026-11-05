namespace OMP.LSWTSS.CApi1;

public interface IClassRegisteredMethodSchema : IClassRawMethodSchema
{
    public string ApiFunctionName { get; set; }

    public uint? NativeDataRawPtrOffset { get; set; }
}