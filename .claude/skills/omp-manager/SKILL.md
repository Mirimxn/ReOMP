---
name: omp-manager
description: Plan, build or change the ReOMP Mod Manager desktop app, including its UI, OMP setup, mod installation and input settings.
---

# Work on the Mod Manager

Work in `apps/mod-manager/`. The React interface lives in `src/` and the Tauri application in `src-tauri/`. Keep platform-specific file operations separate from the UI so the same actions work on Windows and Linux.

Route drag-and-drop and the file picker into the same package import operation. Use the manifest and dependency rules in `omp-mod` when working on package validation. OMP resolves dependencies by installed folder ID, so validate against the proposed installed and enabled configuration.

Set up the installed ZIP folder and store its path in the manager's global configuration. Keep the archive inventory separate from enablement. Watch the folder for changes so the UI updates while open. Follow `notes/omp/mod-packages.md` for package layout, metadata and event-driven inventory updates.

Validate before installing, updating, enabling, disabling or removing mods. Missing dependencies, conflicts and incompatibilities block the change. Show the affected mods and the reason in the UI. Leave the current configuration and game files intact when validation fails.

The input editor reads and writes the same saved bindings as the in-game menu. Load `omp-mod` before changing the shared binding contract.

Run `npm run build` from `apps/mod-manager/` to check the frontend. Verify install changes against a temporary game directory. Exercise both import methods and check that a rejected change leaves the previous configuration intact. Report which platforms were checked.
