# Asset replacement

Provide one owner for registrations that add or replace game resources. Start from [the v1 manifest action](../../mod-sources/v1/v1/src/ModRegisterCustomResourceAction.cs) and [its file-linking implementation](../../mod-sources/v1/v1/src/LinkCustomResource.cs).

## Package contract

Use the provider ID `asset-replacement` and initial version `1.0.0`. A consumer declares `{"id":"asset-replacement","minVersion":"1.0.0"}` in `dependencies`. Use one action per resource.

```json
{
  "typeId": "asset-replacement/register",
  "payload": {
    "path": "chars/example/example.prefab_baked",
    "srcPath": "assets/example.prefab_baked"
  }
}
```

`path` is the canonical game resource path. Normalise to forward slashes and compare without case. `srcPath` is a file inside the mod folder. Both the manager and provider validate the same shape. [The manager's Game assets view](../mod-manager.md#game-assets-and-replacement-mods) generates this action and uses it to check concrete conflicts before deployment.

## Implementation sequence

1. Extract the existing asset lookup behaviour into the provider. Collect declarations during manifest loading. Use the inherited file-exists, position and open hooks to route declared resources through the game's loose-file device. Preserve the original hooks' behaviour for all other paths. Verify one replacement and one added resource on each available Windows variant. [Inherited open hook](../../mod-sources/v1/v1/src/CApi1NuFileDeviceDatCreateFileMethodHook.cs)
2. Have the manager materialise the declared files in the game's asset directories as part of its normal deployment transaction. Stage copies and publish them using the existing receipt and rollback design. The provider checks that the manager's file exists and registers the lookup. Replace the inherited runtime hard-link and delete step for manifest-declared resources.
3. Reject unsafe paths, duplicate destinations, and targets outside recognised game asset directories. Use the asset catalogue for replacements and verify the allowed directories for added assets against the existing character and vehicle fixtures. Treat archive containers, executables and OMP files as reserved. When replacing an existing loose game asset, preserve its bytes in the manager's tracked backup and restore them on removal. Block if a destination or receipt has changed since preflight. Keep original DAT and DAT2 files read-only.
4. Migrate one asset-only package, then have the [character](character-registration.md) and [vehicle](vehicle-registration.md) providers use the same contract for packaged assets.

## Acceptance

A fixture supplies one replacement and one new resource. A duplicate destination is reported with both owning mods. Turning the consumer off restores the original game lookup and preserves unrelated loose files. Check native path behaviour on Windows before Linux users verify it under Proton.
