---
name: omp-mod
description: Plan, build or change an OMP mod, including gameplay behaviour, player controls, manifests, dependencies and APIs used by other mods.
---

# Build an OMP mod

## Put game access in a shared provider

Before writing feature code, identify the game operations it needs. Check existing providers in `mod-sources/`. Start with `mod.md` where available, then verify the provider's manifest, exported actions or API and support for the target build. Use `plans/mod-migration.md` to find the planned owner when a provider is missing.

Use a provider for direct game operations such as finding or changing entities, spawning NPCs, loading scenes, intercepting game functions and replacing assets. If an operation is missing, extend the provider that already owns that part of the game. Create an ordinary OMP dependency mod when there is no suitable owner. Add only the reusable operation the feature needs and declare the provider as a direct dependency. Keep the feature's rules, sequencing, assets and presentation in the feature mod.

Use a manifest-only package when an existing action provides the requested behaviour. Add a .NET 8 scripting module when the mod needs its own code.

## Make shared contracts safe for multiple consumers

- Give each game hook, resource registry or mutable system one provider that owns the game-facing operation. Expose typed operations, subscriptions or manifest actions. Keep native addresses, game-thread scheduling and handle validation behind that boundary.
- Scope registrations, subscriptions and persistent data to the declaring mod and a stable ID. Return a handle or token when a consumer needs to release its contribution. Make removal affect only that owner's contribution. Reject stale entity or scene handles before using them.
- Specify what happens when two mods use the same capability. Independent contributions should coexist. For the same target, define the supported order or combination rule. Reject a second claim with both owners identified when the operation is exclusive. Avoid accidental priority from manifest traversal, silently replacing a registration, or restoring a stale value over another mod's change.
- Validate declarations before touching game state. For conflicts visible in manifests, expose the concrete target in the action contract so the manager can detect it before deployment. Validate runtime requests again in the provider because live targets and registrations can change.
- Document the public API, ownership, order, cleanup and error behaviour in the provider's `mod.md`. Preserve existing contracts for dependants when publishing updates. Verify with two independent consumers enabled together, including a shared target, removal of one consumer and a rejected conflicting request.

## Manifest and dependencies

OMP reads `<game>/mods/<mod-id>/mod.json` at startup. The folder name is the mod ID. Use `name` for the display name and `description` for a short summary. Older manifests can omit `description`. Supply `actions` as an array of `{"typeId": "...", "payload": {...}}` objects and `dependencies` as an array of `{"id": "..."}` objects. Use an empty array when there are no dependencies. Keep package file paths relative to the mod folder and match the packaged file names.

Declare a direct dependency on every mod whose actions or APIs you use. The dependency ID is the provider's exact, case-sensitive folder name. Providers must already be installed. The current dependency format contains only `id`.

OMP loads dependencies recursively and executes each mod's actions in array order. A provider's action registrations are therefore available when a dependent's actions run. Missing dependencies, dependency cycles and unknown action types are startup errors. Scripting module constructors run after manifest actions.

For C# APIs, also reference the provider assembly in the mod's project. If the provider registers that DLL as a shared assembly, use `<Private>false</Private>` on the reference so the provider supplies the DLL at runtime.

## Use an existing action

`v1` provides actions for custom resources, characters and vehicles. Start with the matching action below. If a character or vehicle uses a packaged prefab, register its resources before the action that refers to them.

| Action ID | Payload |
| --- | --- |
| `v1/register-custom-resource-action` | `path` is the game's resource path. `srcPath` is the packaged file path relative to the mod folder. |
| `v1/register-custom-character-action` | See `mod-sources/v1/v1/src/ModRegisterCustomCharacterActionPayload.cs`. |
| `v1/register-custom-vehicle-action` | See `mod-sources/v1/v1/src/ModRegisterCustomVehicleActionPayload.cs`. |

Example manifest using an existing game prefab. The only direct dependency is `v1`.

```json
{
  "name": "Republic Gunship",
  "description": "Adds the Republic Gunship to the vehicle roster.",
  "dependencies": [{ "id": "v1" }],
  "actions": [
    {
      "typeId": "v1/register-custom-vehicle-action",
      "payload": {
        "id": "Republic_Gunship",
        "templateMatch": "Wookie_Gunship",
        "prefabResourcePath": "chars/vehicles/republic_gunship/republic_gunship.prefab_baked"
      }
    }
  ]
}
```

## Add code or expose an API

Build the module for `net8.0`. Give its class a public parameterless constructor and register the compiled assembly using `register-scripting-module-action`. `typeName` is the fully qualified class name. `assemblyPath` is the DLL path relative to the manifest.

```json
{
  "typeId": "register-scripting-module-action",
  "payload": {
    "typeName": "ExampleMod.EntryPoint",
    "assemblyPath": "ExampleMod.dll"
  }
}
```

OMP creates the module instance and the constructor sets up the mod. For a module that supports unloading, implement `IDisposable` and release its hooks and subscriptions in `Dispose`. See `mod-sources/test-cef-mod/test-cef-mod/src/TestCefMod.cs` and `workspaces/dotnet/dev-tools/src/BuildTestCefMod.cs` for the lifecycle and packaging pattern.

For APIs consumed by other mods, use `register-shared-assembly-action` with `name` and `path`. To expose a manifest action, register the shared assembly first, then use `register-mod-action-type-action` with `id`, `executeModActionSharedAssemblyName` and `executeModActionStaticTypeName`. The handler is a public static `void Execute(string modId, string modDirPath, string payloadJson)` method. See `workspaces/dotnet/dev-tools/src/BuildV1.cs` for both registrations.

## Player controls and verification

Register player-facing actions through the shared input mod and preserve action IDs used by saved bindings. Use the published input API when it is available. If that API is still planned, identify it as a prerequisite for the feature. For a change to the shared binding format, account for both editors and load `omp-manager` before changing the desktop editor.

Stage `mod.json`, any DLLs and all referenced assets together in a folder named with the mod ID. Verify the feature through OMP's normal load path with the declared dependencies installed. For an API change, check a consuming mod as well.

Put mod source projects in `mod-sources/<mod-id>/`. Use an optional `mod.md` at the source root to document the mod's purpose and public contracts. Include it beside `mod.json` in the single mod folder inside the ZIP. See `notes/omp/mod-packages.md` for the package format.

Runtime sources are in `workspaces/dotnet/runtime-engine/src/`. Start with `RegisterMods.cs` for discovery, `LoadModIfUnloaded.cs` for dependency order and `ModActionTypesInfo.cs` for built-in actions.
