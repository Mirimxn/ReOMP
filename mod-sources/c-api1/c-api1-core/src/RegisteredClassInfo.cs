using System.Linq;

namespace OMP.LSWTSS.CApi1;

public sealed class RegisteredClassInfo(string apiClassName) : IRegisteredClassInfo
{
    public string ApiClassName { get; private set; } = apiClassName;

    ApiClass.NativeHandle _cachedApiClass;

    public ApiClass.NativeHandle GetApiClass()
    {
        if (_cachedApiClass == nint.Zero)
        {
            _cachedApiClass = ApiClass.Instances.FirstOrDefault(apiClass => apiClass.AsApiType().GetName() == ApiClassName);
        }

        return _cachedApiClass;
    }
}
