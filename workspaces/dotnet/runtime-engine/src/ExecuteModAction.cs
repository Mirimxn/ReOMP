namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void ExecuteModAction(string modId, string modDirPath, ModActionInfo modActionInfo)
    {
        var modActionTypeInfo = _modActionTypesInfo.Find((modActionTypeInfo) => modActionTypeInfo.Id == modActionInfo.TypeId);

        if (modActionTypeInfo == null)
        {
            throw Crash($"Cannot find action of type {modActionInfo.TypeId} for {modId}");
        }

        modActionTypeInfo.ExecuteModAction(modId, modDirPath, modActionInfo.Payload.ToString());
    }
}