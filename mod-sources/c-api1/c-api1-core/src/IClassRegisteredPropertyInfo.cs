namespace OMP.LSWTSS.CApi1;

public interface IClassRegisteredPropertyInfo
{
    public IRegisteredClassInfo Class { get; }

    public string ApiClassFieldName { get; }

    ApiClassField.NativeHandle GetApiClassField();
}
