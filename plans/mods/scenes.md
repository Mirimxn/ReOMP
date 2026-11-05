# Gameplay scene loading

Provide an API for requesting a gameplay level transition. The donor checkout at `D:\TSS Emotes\omp-lswtss-jubjub` contains two starting points.

| Donor source | Relevant operation |
| --- | --- |
| `workspaces/dotnet/level-loader-mod/src/LevelLoaderMod.cs` | `ProcessPendingLevelLoadRequest` resolves the active world and universe, then calls `GameFlowUniverseSystem.JumpToLevelFromFullPath` with `levels/test/copilottest/copilottest.scene_baked`. |
| `workspaces/dotnet/test-cef-mod/src/LoadLevelExample.cs` | Shows `GameProgressSystem.EnterLevel` by name and a full-path helper that accepts optional arrival point and world names. |

These are source references. Successful level changes and their effect on save progress still need to be verified on ReOMP.

## Implementation sequence

1. Compare donor binding definitions with ReOMP's Steam and Epic bindings. Port the smallest level request from the donor, using [game access](game-access.md) for game-thread execution and world lookup. Keep the donor's diagnostic input and animation code outside this provider.
2. Use `JumpToLevelFromFullPath` as the public transition path. The caller supplies a gameplay scene resource path that the game can resolve. A built-in scene and a packaged scene use the same request. A packaged scene and its dependencies must already be visible through the [asset provider](asset-replacement.md). Keep `EnterLevel` as a donor reference for debugging normal game flow.
3. With a disposable save, verify transition start, completion, player arrival and whether the game writes progress. Expose a request result that distinguishes rejection, accepted transition and confirmed arrival. Reject a second request during a transition and invalidate world-scoped handles when the game changes worlds.

## Acceptance

Demonstrate a verified transition into a known gameplay level and out again. Exercise an invalid destination, a repeated request and a request while the game world is unavailable. Record the outcome and save behaviour per tested variant. When a playable packaged scene fixture exists, run the same checks on its resource path.
