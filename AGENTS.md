# ReOMP maintainer guide

ReOMP is a monorepo for the game runtime, core mods, platform installers and desktop mod manager.

## Repository layout

| Path | What belongs there |
| --- | --- |
| `workspaces/rust/` | Native OMP loader, `dinput8.dll` proxy and hook code. |
| `workspaces/dotnet/` | OMP runtime, API generation and build tools. |
| `mod-sources/` | Source projects for authored mods, one directory per mod. |
| `installers/linux/` | Linux installer work. Contains the inherited Steam Deck test setup script. |
| `installers/windows/` | Windows installer for the desktop mod manager. |
| `config/` | ReOMP global configuration schema. |
| `apps/mod-manager/` | Standalone desktop mod manager. React source is in `src/` and the Tauri shell is in `src-tauri/`. |
| `scripts/` | OMP release packaging. Start with `package-release.ps1`. |
| `notes/` | Maintainer notes by project. Use `omp/`, `installer/` or `mod-manager/` inside this folder. |
| `plans/` | ReOMP feature and implementation plans. |

## Active implementation roadmap

When asked about the plan, what work remains or what to do next, start with the [ReOMP implementation roadmap](plans/roadmap.md). Use its status and dependencies to offer up to three plain-language choices, recommend one and ask which to take. If the developer has already chosen, follow the linked plan. Record progress and verification in the roadmap after each work session.

Remove this section once the roadmap's completion checks pass.

## Product rules

- Core mods are ordinary OMP mods that provide shared functionality through dependencies. All mods use the same manifest, dependency and loading rules.
- Put direct game integration in shared core mods. Feature mods use those providers through declared dependencies and implement their own gameplay rules. Check existing providers before extending one or creating a new dependency.
- Shared providers own their game-facing hooks, resources and mutations. Define how multiple consumers coexist and how each consumer releases its own state. Reject changes that cannot be combined safely.
- Encourage an optional `mod.md` at the mod's source root. Document the mod's purpose and public API contracts there. Include it beside `mod.json` in the package.
- Every mod feature that responds to player controls uses the shared input mod. The single in-game input menu and the Mod Manager editor work with the same saved bindings.
- Platform installers set up the ReOMP Mod Manager. The cross-platform manager installs OMP into the game and manages mods through its UI. Drag-and-drop and the file picker use the same import flow.
- The Mod Manager validates the resulting configuration before applying changes. Missing dependencies, conflicts and incompatibilities are blocking errors. Show the reason and leave the existing configuration intact.
- The Mod Manager sets up its installed-mod folder and records its path in ReOMP's global config. The folder holds ZIP packages with one mod folder each. The manager watches it for live inventory changes and controls enablement through its UI. See `notes/omp/mod-packages.md`.

## Skills

Load the relevant skill before planning or changing its component. Select skills from the work the request entails. Load each applicable skill when the work spans components.

| Work | Skill |
| --- | --- |
| OMP mods, including gameplay, manifests, dependencies and APIs provided by a mod | `omp-mod` |
| OMP loader, runtime, manifest processing, game bindings and native hooks | `omp-runtime` |
| Desktop UI, game setup, mod management and desktop input settings | `omp-manager` |
| Packaging and installing the Mod Manager on a target platform | `omp-installer` |

## Build and verify

| Working directory | Command | What it checks |
| --- | --- | --- |
| `workspaces/dotnet/dev-tools/` | `dotnet run --configuration Release -- build-v1` | Builds the core registration module against the API and hook assemblies in `dist/`. |
| `apps/mod-manager/` | `npm ci`, then `npm run build` | Installs locked dependencies and checks the React frontend. |
| `apps/mod-manager/` | `npm run tauri build -- --debug --no-bundle` | Compiles the desktop app without creating an installer. |
| Repository root | `bash -n installers/linux/setup-linux.sh` | Checks the Steam Deck script syntax. |

Run OMP dev-tool commands from their own directory. They resolve source and output paths from the current working directory.
