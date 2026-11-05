# Game access

Provide stable, typed access to the active game world and player entities. Start with [the raw bindings](../../mod-sources/c-api1/mod.md), [world lookup in v1](../../mod-sources/v1/v1/src/GetCApi1CurrentApiWorld.cs) and [player lookup in Galaxy Unleashed](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/FetchPlayerEntity.cs).

## Implementation sequence

1. Use the existing variant bindings to resolve the current world. Verify when world and player access becomes safe during load, respawn, character swap and scene exit. The donor player helper selects index zero. Check what the game exposes for other player slots before publishing a public player-selection contract.
2. Establish one supported point for work on the game's update thread. Let consumers query the active world, resolve a player, read an entity transform and observe world changes through that service. Ensure handles from a previous world or destroyed entity are rejected before any native call.
3. Add typed player operations as they are verified. [JumpBooster](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/JumpBooster.cs) shows a candidate read/write/restore path for jump settings. Extract the underlying operation independently of the boost feature. Verify whether edits affect an entity instance or a shared game definition.
4. Use the same entity and lifetime contract for NPCs and animations. Extract the asynchronous prefab loader and resource ownership from [PrefabResource](../../mod-sources/v1/v1/src/PrefabResource.cs) so spawn and roster providers can share it. Keep the [NPC stats](npc-stats.md) contract responsible for NPC-specific fields.

## Acceptance

A consumer resolves a player and transform in gameplay, receives a defined unavailable result during transitions, and rejects a stale handle after a character or world change. Demonstrate one reversible player stat edit and verify whether another entity is affected.
