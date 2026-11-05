---
name: omp-runtime
description: Plan, build or change core OMP functionality, including the native loader, .NET runtime, mod discovery and manifest contracts, game bindings, and hook infrastructure.
---

# Work on the OMP runtime

| Area | Entry points |
| --- | --- |
| Startup and game variant selection | `workspaces/rust/driver-dinput8-library/src/lib.rs` loads the driver. See `get_variant.rs` and `init_engine.rs` in `workspaces/rust/driver-library/src/`. |
| Manifest and dependency handling | `ModInfo.cs`, `RegisterMods.cs` and `LoadModIfUnloaded.cs` in `workspaces/dotnet/runtime-engine/src/`. |
| Action and module loading | `ModActionTypesInfo.cs` and `LoadScriptingModuleIfUnloaded.cs` in the runtime source directory. |
| Game bindings and hook infrastructure | `mod-sources/c-api1/c-api1-main/src/` for schemas, `workspaces/dotnet/dev-tools/src/BuildCApi1.cs` for generation, and `mod-sources/c-func-hook1/c-func-hook1/` and `mod-sources/c-func-hook1/c-func-hook1-native/` for hooks. |

Treat manifest fields, action IDs and payloads, dependency ordering, and module entry points as contracts with installed mods. Check existing consumers before changing a contract. Keep older manifests readable when adding optional metadata. When a runtime change affects package validation, load `omp-manager` and check that its rules match the runtime contract.

For game binding or native hook changes, verify against the executable for the targeted game variant and update the relevant binding inputs. Build from `workspaces/dotnet/dev-tools/` and verify the change through the normal OMP load path with a representative mod. Report build and in-game verification separately.
