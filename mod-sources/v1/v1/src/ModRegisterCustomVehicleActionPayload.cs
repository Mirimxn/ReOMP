using System.Collections.Generic;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    class ModRegisterCustomVehicleActionPayload
    {
        // Unique id for the new roster entry (also used to derive its GUID).
        [JsonProperty("id")]
        public required string Id { get; set; }

        // Collectables table that holds the vehicle roster.
        [JsonProperty("tableName")]
        public string TableName { get; set; } = "Vehicles";

        // Entry name of the row to clone (exact), or a case-insensitive substring of its
        // prefab/graph path, e.g. "wookie_gunship".
        [JsonProperty("templateMatch")]
        public required string TemplateMatch { get; set; }

        // Prefab the new entry should spawn.
        [JsonProperty("prefabResourcePath")]
        public required string PrefabResourcePath { get; set; }

        // Optional exact replacements applied to every string and graph path
        // copied from the template (old value -> new value, case-insensitive).
        [JsonProperty("replacements")]
        public Dictionary<string, string> Replacements { get; set; } = [];

        // Optional per-column overrides, keyed by column name (e.g. "Price") or index, applied last.
        // Value is parsed to the column's type (string, int, float, bool, enum index, graph path).
        [JsonProperty("columnOverrides")]
        public Dictionary<string, string> ColumnOverrides { get; set; } = [];

        // Display name / description shown in game (registered as custom strings).
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        // Unlock it (Collect State = unlockedCollectState) and make it free.
        [JsonProperty("unlocked")]
        public bool Unlocked { get; set; } = true;

        [JsonProperty("unlockedCollectState")]
        public int UnlockedCollectState { get; set; } = 2;

        // PNG icon for the roster, relative to the mod folder (any size; fitted into 256x256).
        [JsonProperty("icon")]
        public string? Icon { get; set; }

        // Writes detailed information (table columns, template row, ...) to omp-custom-vehicles.log.
        [JsonProperty("debugLog")]
        public bool DebugLog { get; set; } = false;

        // When true, only logs the tables and the template row; nothing is added.
        [JsonProperty("dryRun")]
        public bool DryRun { get; set; } = false;

        // Optional roster preview: a .SCENE_BAKED (a vehicle "cheap" scene) relative to the mod folder.
        // The game only loads previews from paths it already knows, so the scene is served in place of
        // the unused preview of one of the game's hidden roster entries (chosen automatically).
        [JsonProperty("previewScene")]
        public string? PreviewScene { get; set; }

        // Optional: the game roster entry whose preview slot shows this vehicle's preview (default: the template).
        // Give each custom vehicle a different one, e.g. a microfighter, so switching between them reloads.
        [JsonProperty("previewBase")]
        public string? PreviewBase { get; set; }

        // Serve the preview scene exactly as it is, without renaming its header to the borrowed slot.
        [JsonProperty("previewKeepHeader")]
        public bool PreviewKeepHeader { get; set; } = false;
    }
}
