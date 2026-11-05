# OMP mod migration plan

Migrate reusable capabilities into ordinary OMP mods under `mod-sources/<mod-id>/`. Each provider publishes a versioned ZIP and documents the API or manifest actions it supplies. A consuming mod declares its direct dependencies with minimum versions. The [Mod Manager plan](mod-manager.md) owns player deployment and legacy ZIP or folder recovery.

## Individual plans

| Plan | Starting point | First dependency to verify |
| --- | --- | --- |
| [Hooks](mods/hooks.md) | `c-func-hook1` managed wrapper and Rust detours | Native hook attachment and cleanup |
| [Game access](mods/game-access.md) | `c-api1` bindings, `v1` world lookup, Galaxy Unleashed player access | Bindings for the Steam and Epic executables |
| [Input](mods/input.md) | `input-hook1` window-message capture | Hooks and an offline action catalogue for the manager |
| [In-game UI](mods/ui.md) | `overlay1` and its input and render lifecycle | Shared input and DX11 rendering |
| [Asset replacement](mods/asset-replacement.md) | `v1` resource registration and lookup | Safe redirection or tracked materialisation |
| [Character registration](mods/character-registration.md) | `v1` character registration and roster lookup | Game bindings and asset replacement for custom assets |
| [Vehicle registration](mods/vehicle-registration.md) | `v1` vehicle registration and workspace gunship fixtures | Game bindings and asset replacement for custom assets |
| [Vehicle spawning and control](mods/vehicle-spawning.md) | `v1` vehicle definitions and native roster spawn tracing | Vehicle lookup, prefab lifetime and verified seat transitions |
| [NPC spawning](mods/npc-spawning.md) | Galaxy Unleashed and Test CEF prefab and NPC lifecycles | Game access and asynchronous prefab loading |
| [NPC orders](mods/npc-orders.md) | Galaxy Unleashed navigation and opponent selection | Game access and verified NPC navigation and combat calls |
| [NPC stats](mods/npc-stats.md) | Existing NPC faction and movement code provides leads | Game access and a verified setter for each stat |
| [Animations](mods/animations.md) | NPC respawn playback and TSS Emotes playback code | Game access and verified animation entry points |
| [Scene loading](mods/scenes.md) | Jubjub's `level-loader-mod` and named-level example | Game access, level bindings and transition detection |
| [Persistent storage](mods/storage.md) | New SQLite-backed service | Native SQLite resolution from an OMP package |

## Migration order

1. **Establish the dependencies.** Validate hooks and game access first. Keep the raw `c-api1` bindings available during extraction. Record the game build behind each binding artefact. The generator currently takes both the Steam and Epic game directories. Where an executable is unavailable, use a known matching build artefact for compilation and record in-game verification as pending for that variant. Storage can proceed independently.
2. **Extract by capability.** Check the inherited manifest and its consumers when replacing a provider. Record the source revision and licence when importing donor code. The current local donors for scenes and animation are under `D:\TSS Emotes`. Move raw input capture into input and take the DX11 renderer into UI. Register input actions through the shared input API. Move `v1` resource registration, character registration and vehicle registration into their respective providers. Extract world, player, prefab and NPC code only where a planned API uses it. Migrate animation and scene playback from the donor paths listed in their plans. Keep a passing consumer for each extracted capability before moving the next caller.
3. **Verify package contracts.** Build each provider with the existing .NET dev tools and package it using the normal single-folder ZIP layout. Check `mod.json` version and dependency minima against the manager plan. Use `mod.md` to document provider entry points, thread and lifetime rules, and a working consumer example. Consumers reference provider assemblies without bundling duplicate copies. OMP registers manifest actions before scripting module construction, so a provider action can collect declarations at startup and defer game calls until a world is available. [Action registration](../workspaces/dotnet/runtime-engine/src/ModActionTypesInfo.cs) · [Dependency loading](../workspaces/dotnet/runtime-engine/src/LoadModIfUnloaded.cs)
4. **Cut over the release.** Test each migrated consumer against the new packages from a clean staging directory. Update `BuildAll.cs`, `BuildAllNoCApi1.cs`, the release packaging list and skill examples as each provider replaces its predecessor. Audit the output for inherited providers pulled in by the current seed-bundle step and for duplicate native hooks. Use the manager's legacy backup flow for old installed packages. [Release packager](../scripts/package-release.ps1)
5. **Archive the inherited sources.** After the replacement stack builds, packages and passes the in-game acceptance checks, move the retired OMP mod source trees to `legacy/mod-sources/<old-mod-id>/`. For an ID retained by a migrated provider, keep its active source under `mod-sources/` and archive its source from the commit before migration. Remove retired build entries, point current examples at the active providers and update donor references to their archive paths. Run a clean release build after the move and verify it uses only active sources and emits only the intended provider ZIPs.

## Disposition of inherited mods

| Existing mod | Migration |
| --- | --- |
| `c-func-hook1` | Extract the hook provider and verify each consumer on the new hook lifecycle. |
| `c-api1` | Preserve the generated binding pipeline and ship the existing ID as an ordinary dependency beneath game access and providers that need verified raw bindings. |
| `input-hook1`, `overlay1` | Reuse proven capture and render pieces in the input and UI providers. Migrate both together at the integration boundary. |
| `v1` | Split into game access, asset replacement, character registration and vehicle registration. Put prefab loading and lifetime in game access so roster and spawn code share it. |
| `galaxy-unleashed` | Extract reusable player and NPC code as evidence. Retire the gameplay mod and its build entry once extraction is complete. |
| `test-cef-mod` | Use as a reference while extracting UI and NPC code. Replace its coverage with small consumer fixtures kept out of the player release. |
| `debug-tools` | Keep developer reload out of the player release. Use it against the inherited stack while migrating. Treat reload of the new providers as a separate verification task. |

Record Windows build, package and in-game results separately for Steam and Epic. Linux runtime results are deferred until Linux users can check the Windows game and native dependencies under Proton.
