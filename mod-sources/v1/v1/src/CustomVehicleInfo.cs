using System.Collections.Generic;

namespace OMP.LSWTSS;

public static partial class V1
{
    class CustomVehicleInfo
    {
        public required string Id { get; set; }

        // Name of the roster entry inside the game. Prefixed so a mod id can never
        // collide with one of the game's own vehicles (e.g. "Republic_Gunship").
        public string EntryName => $"OMP_{Id}";
        public required string TableName { get; set; }
        public required string TemplateMatch { get; set; }
        public required string PrefabResourcePath { get; set; }
        public required Dictionary<string, string> Replacements { get; set; }
        public required Dictionary<string, string> ColumnOverrides { get; set; }
        public required bool DryRun { get; set; }
        public string? PreviewScenePath { get; set; }
        public string? PreviewBase { get; set; }
        public bool PreviewKeepHeader { get; set; }
    }

    static readonly List<CustomVehicleInfo> _customVehiclesInfo = [];

    static readonly HashSet<string> _processedCustomVehicleTables = [];
}
