namespace OMP.LSWTSS.CApi1;

public interface IClassRegisteredMethodInfo : IClassMethodInfo
{
    public IRegisteredClassInfo Class { get; }

    public string ApiFunctionName { get; }

    ApiFunction.NativeHandle GetApiFunction();
}
