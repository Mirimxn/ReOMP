namespace OMP.LSWTSS.CApi1;

public interface IRegisteredClassInfo : IClassInfo
{
    public string ApiClassName { get; }

    public ApiClass.NativeHandle GetApiClass();
}
