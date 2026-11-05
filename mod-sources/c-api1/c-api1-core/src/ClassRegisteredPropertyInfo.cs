using System.Linq;

namespace OMP.LSWTSS.CApi1;

public sealed class ClassRegisteredPropertyInfo(IRegisteredClassInfo class_, string apiClassFieldName) : IClassRegisteredPropertyInfo
{
    public IRegisteredClassInfo Class { get; private set; } = class_;

    public string ApiClassFieldName { get; private set; } = apiClassFieldName;

    ApiClassField.NativeHandle _cachedApiClassField;

    public ApiClassField.NativeHandle GetApiClassField()
    {
        if (_cachedApiClassField == nint.Zero)
        {
            var apiClass = Class.GetApiClass();

            if (apiClass == nint.Zero)
            {
                return (ApiClassField.NativeHandle)nint.Zero;
            }

            _cachedApiClassField = apiClass.GetFields().FirstOrDefault(apiClassField => apiClassField.GetName() == ApiClassFieldName);
        }

        return _cachedApiClassField;
    }
}
