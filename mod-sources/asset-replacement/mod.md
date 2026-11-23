# Asset Replacement

Routes the game's lookups for mod-supplied game files to the copies in the game folder. It is the only mod that hooks the game's archive file device (`NuFileDeviceDat` create-file and get-position, `NttResourceHandle::IsFileExists`, and the device constructors). Other mods use the API below instead of hooking those functions. [Plan](../../plans/mods/asset-replacement.md)

## Manifest action

```json
{ "typeId": "asset-replacement/register", "payload": { "path": "chars/example/example.texture", "srcPath": "files/CHARS/EXAMPLE/EXAMPLE.TEXTURE" } }
```

Declare `{"id": "asset-replacement", "minVersion": "1.0.0"}` as a dependency. `path` is the game resource path; it is compared without case and with `/` separators. `srcPath` is the packaged file relative to the mod folder.

The ReOMP Mod Manager places the file at `path` in the game folder when it installs the mod, keeps any loose original aside and restores it when the mod is removed. At startup this mod only registers the lookup. If nothing is at `path` (an install made without the manager), the packaged file is copied there; an existing file is never deleted or overwritten.

A path belongs to the first mod that registers it. A later registration from another mod is rejected and logged with both mod IDs; the manager blocks such conflicts before deployment.

## C# API

Reference `omp-lswtss-asset-replacement.dll` with `<Private>false</Private>`. All members are on `OMP.LSWTSS.AssetReplacement`.

| Member | Behaviour |
| --- | --- |
| `InitIfNotInitialized()` | Enables the file hooks. Called by this mod's module; safe to call again. |
| `Register(owner, path, out existingOwner)` | Routes `path` to the game folder's file for `owner`. Returns `false` and the current owner if another owner holds it. Registering again for the same owner succeeds. |
| `Unregister(owner, path)` | Removes `owner`'s registration only. |
| `IsRegistered(path)` | Whether a lookup for `path` is routed. |
| `RegisterModFile(modId, modDirPath, path, srcPath)` | What the manifest action does, for providers that forward their own resource actions. |
| `AddRequestObserver(owner, Action<string> observer)` | Called with the requested path for every file the game opens from its archives. Returns an `IDisposable` that removes it. |
| `AddHideFilter(owner, Func<string, bool> isHidden)` | Given the canonical path; returning `true` makes the game's lookup fail as if the file didn't exist. Returns an `IDisposable`. |

Observers and filters run on the game's loading thread in registration order. An exception in one is logged and doesn't affect the others or the game's lookup.
