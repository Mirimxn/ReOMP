# NPC spawning

Provide a reusable operation to spawn an NPC and manage its lifetime. Use [SpawnNpcTask](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/SpawnNpcTask.cs), [Npc](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/Npc.cs) and [PrefabResource](../../mod-sources/v1/v1/src/PrefabResource.cs) as references.

## Implementation sequence

1. Prove asynchronous loading and cloning of one known character prefab on the game thread. Use [game access](game-access.md) for prefab loading, the target world and entity lifetime. Take a character ID and resolve it through the roster lookup from the [character registration plan](character-registration.md).
2. Track each request from submission to a spawned entity or a defined failure. Release the prefab and remove any partial entity on failure. Cancel a pending request when its world changes or its owner goes away. Tie the returned entity to its world and define how the caller removes a spawn it owns.
3. Separate prefab loading, placement and cleanup from gameplay policies such as waves, battles, spawner placement and automatic respawns. Use the shared entity handle with [NPC stats](npc-stats.md) and [NPC orders](npc-orders.md).

## Acceptance

A fixture spawns one NPC at a supplied location, receives a valid entity, then removes it. Verify an invalid prefab, repeated spawns and a scene transition during loading. Confirm no pending request places an NPC in the next scene.
