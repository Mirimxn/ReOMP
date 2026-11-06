# V1

Registers custom game resources, characters and vehicles. Depend on `v1` and use `v1/register-custom-resource-action`, `v1/register-custom-character-action` or `v1/register-custom-vehicle-action` in the manifest. The payload contracts live in the matching `ModRegisterCustom*ActionPayload.cs` files under `v1/src/`.

Managed mods can also reference the shared `omp-lswtss-v1` assembly for APIs such as `V1.PrefabResource` and `V1.FetchCharactersInfo`.

## Vehicle action

`v1/register-custom-vehicle-action` copies an existing roster row (the template) into a new row named `OMP_<id>` that spawns another prefab. Ship the prefab and its files with `v1/register-custom-resource-action`.

```json
{
  "typeId": "v1/register-custom-vehicle-action",
  "payload": {
    "id": "My_Vehicle",
    "templateMatch": "Wookie_Gunship",
    "prefabResourcePath": "chars/vehicles/my_vehicle/my_vehicle.prefab_baked",
    "name": "My Vehicle",
    "description": "A custom vehicle.",
    "icon": "icon.png",
    "previewScene": "PREVIEW/MY_VEHICLE_PREVIEW.SCENE_BAKED",
    "previewBase": "Micro_Vulture_Droid"
  }
}
```

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | yes | Stable ID. The row is added as `OMP_<id>` and the game saves progress by it. |
| `templateMatch` | yes | Exact roster entry name, or text contained in its prefab path. Use a playable entry; hidden rows such as `Republic_Gunship` make the copy hidden. |
| `prefabResourcePath` | yes | Prefab the row spawns. |
| `name`, `description` | no | Text served through `OMP_VEHICLE_<ID>` string keys. |
| `icon` | no | PNG in the mod folder, fitted into a cell of the roster's icon sheet (256 px for vehicles, 128 px for characters). |
| `previewScene` | no | Cheap scene (`.scene_baked`) in the mod folder for the roster preview. Without it the template's preview shows. |
| `previewBase` | no | Roster entry whose preview slot serves `previewScene`. Defaults to the template. Must have a `.scene_baked` cheap graph. |
| `previewKeepHeader` | no | Serve the scene unchanged instead of patching its header to the base slot. |
| `unlocked` | no | Default `true`: collected and free. |
| `columnOverrides` | no | Column name or index to value. A graph path ending in `@template-id` keeps the template's resource ID. |
| `replacements` | no | Exact old to new strings applied to copied values. |
| `tableName` | no | Default `Vehicles`. `Characters` also sets `Base`, `CurrentlySelectedOutfit` and `BaseNameId` for the new row. |
| `debugLog`, `dryRun` | no | Log columns, the template row, file requests and preview routing. `dryRun` adds nothing. |

Everything is logged to `omp-custom-vehicles.log` in the game folder.

### Previews

The roster draws previews through `UIRenderTargetModelLoader::LoadGraph`, and only for paths already in the game's archives. The row's cheap graph is set to `chars/vehicles/omp_previews/<id>_cheap.scene_baked`. The LoadGraph hook routes that path to the `previewBase` slot and serves a copy of the mod's scene whose header names the slot (self path, source path and source ID from the base row, plus a unique header ID). The file hooks hide the slot's `.res` sidecar while it is overridden. Previewing the base itself restores the original. Two vehicles sharing one base do not reload when switching directly between them, so give each its own base. At startup the module writes the usable bases to `omp-data/vehicle-previews/preview-bases.txt`.

### Icons

Each roster has one icon sheet: vehicles use `ui/icons/vehicles/vehicles.tsh` with `vehicles_dx11.texture` (DXT5, 256 px cells), characters use `ui/icons/characters/characters.tsh` with `characters_dx11.texture` (BC7, 128 px cells, 3712x3712, all 841 cells taken; new icons are encoded with BC7 mode 6). The module reads the original pair from `omp-data/vehicle-icons/` or `omp-data/character-icons/`. If a pair is missing there but an untouched extracted copy is in the game's `UI/ICONS/<roster>` folder, it saves that copy there first. Each `OMP_<id>` icon goes into an empty cell; when none are left the sheet grows by whole rows at the bottom, so no existing icon is moved or covered. The result is cached in `generated/` and the game is redirected to it.

### Asset rules

- Each resource has a 16-byte ID after its self path. Copies need new IDs, including their `.nxg_textures` and `.pc_shaders`, or they clash with the original and render nothing.
- A `.ghg` must reference texture and shader files named after itself, or the game reloads it forever.
- Prefabs store folder and file names separately; keep copies in the original vehicle's folder or rename with equal-length names.
- `tools/preview-scene-maker/` builds a cheap scene for a `.ghg` or prefab from the Wookiee Gunship scene.

Fixtures: `fixtures/vehicles/`.
