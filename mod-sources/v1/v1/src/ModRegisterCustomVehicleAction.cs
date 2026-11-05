using System;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static class ModRegisterCustomVehicleAction
    {
        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            var payload = JsonConvert.DeserializeObject<ModRegisterCustomVehicleActionPayload>(
                modActionPayloadAsJson
            ) ?? throw new InvalidOperationException();

            var overrides = new System.Collections.Generic.Dictionary<string, string>(payload.ColumnOverrides, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(payload.Name))
            {
                var nameId = $"OMP_VEHICLE_{payload.Id.ToUpperInvariant()}";
                RegisterCustomString(nameId, payload.Name);
                overrides.TryAdd("DisplayStringID", nameId);
            }

            if (!string.IsNullOrEmpty(payload.Description))
            {
                var descId = $"OMP_VEHICLE_DESC_{payload.Id.ToUpperInvariant()}";
                RegisterCustomString(descId, payload.Description);
                overrides.TryAdd("DescriptionStringID", descId);
            }

            if (payload.DebugLog || payload.DryRun) _vehicleDebugLog = true;

            if (!string.IsNullOrEmpty(payload.Icon))
            {
                var iconPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(modDirPath, payload.Icon));
                RegisterCustomVehicleIcon(payload.Id, iconPath);
                overrides.TryAdd("IconFileName", GetCustomVehicleIconSpriteName(payload.Id));
            }

            // Characters: make the entry its own roster slot (not an outfit of the template's character).
            // Same columns OMP's register-custom-character-action sets: 7 Base, 8 CurrentlySelectedOutfit, 10 BaseNameId.
            if (string.Equals(payload.TableName, "Characters", StringComparison.OrdinalIgnoreCase))
            {
                overrides.TryAdd("7", $"OMP_{payload.Id}");
                overrides.TryAdd("8", "true");
                if (overrides.TryGetValue("DisplayStringID", out var nameKey)) overrides.TryAdd("10", nameKey);
            }

            if (payload.Unlocked)
            {
                overrides.TryAdd("Collect State", payload.UnlockedCollectState.ToString());
                overrides.TryAdd("Price", "0");
            }

            _customVehiclesInfo.Add(
                new CustomVehicleInfo
                {
                    Id = payload.Id,
                    TableName = payload.TableName,
                    TemplateMatch = payload.TemplateMatch,
                    PrefabResourcePath = payload.PrefabResourcePath,
                    Replacements = payload.Replacements,
                    ColumnOverrides = overrides,
                    DryRun = payload.DryRun,
                    PreviewBase = payload.PreviewBase,
                    PreviewKeepHeader = payload.PreviewKeepHeader,
                    PreviewScenePath = string.IsNullOrEmpty(payload.PreviewScene) ? null : System.IO.Path.GetFullPath(System.IO.Path.Combine(modDirPath, payload.PreviewScene)),
                }
            );

            VehicleLog($"Registered custom vehicle '{payload.Id}' (table '{payload.TableName}', template '{payload.TemplateMatch}', prefab '{payload.PrefabResourcePath}', icon {(string.IsNullOrEmpty(payload.Icon) ? "none" : payload.Icon)}{(payload.DryRun ? ", DRY RUN" : "")})");
        }
    }
}
