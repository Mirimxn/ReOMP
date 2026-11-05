namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModActionTypeInfo
    {
        public required string Id { get; set; }

        public required ExecuteModActionDelegate ExecuteModAction { get; set; }
    }
}