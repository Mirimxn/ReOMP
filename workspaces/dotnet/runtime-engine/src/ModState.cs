namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ModState
    {
        public required string Id { get; set; }

        public required string DirPath { get; set; }

        public required ModInfo Info { get; set; }

        public required bool IsBeingLoaded { get; set; }

        public required bool IsLoaded { get; set; }
    }
}