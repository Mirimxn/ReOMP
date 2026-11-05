# Mod packages

## Source and package layout

- Put authored mod projects under `mod-sources/<mod-id>/`.
- Each mod source root can include an optional `mod.md`. Summarise what the mod does and document the public contracts other mods can use, such as exported action IDs and payloads or assembly APIs.
- An installed ZIP contains one top-level folder named with the mod ID. That folder contains `mod.json`, `mod.md` if present, and the mod's DLLs and assets. Copy `mod.md` from the source root when packaging.
- `mod.json` uses `name` for display and an optional `description` for a short summary. Use `mod.md` for the fuller overview and API documentation.
- The bundled mod build commands emit archives under `dist/mod-packages/`. Run `dotnet run -- package-mod <mod-id>` from `workspaces/dotnet/dev-tools/` to package an existing built payload in `dist/<mod-id>/`.

## Intended installer and Mod Manager integration

- The Mod Manager sets up the installed-mod archive folder and stores its path as `installedMods` in ReOMP's global config. [Global config schema](../../config/reomp.schema.json)
- The manager uses the configured folder for ZIPs and reflects changes through filesystem notifications. Drag-and-drop and the file picker share an import flow. The UI controls enablement and blocks invalid configurations.
- OMP currently discovers unpacked mod folders in the game's `mods/` directory at startup.
