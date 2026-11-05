# NPC stats

Expose verified reads and edits for NPC attributes through the same entity contract used by [game access](game-access.md). Accept an eligible game NPC or one spawned by the [NPC spawning provider](npc-spawning.md). The inherited [NPC code](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/Npc.cs) demonstrates faction changes and movement requests. It does not establish a general stats API.

## Implementation sequence

1. Start with live NPC health. Find the current and maximum health reads and the game operation for changing current health in `HealthComponent`. Verify the binding on two NPCs using the same prefab. Publish the operation only after confirming it affects one instance and leaves the template alone.
2. Expose a typed health read and a one-time change of current health within the living range. Resolve the entity and component on the game thread and return a defined failure for stale entities or an unavailable binding.
3. Apply requests in game-thread queue order. Each request sees the value that exists when it runs. Let the game continue to update health normally between requests. Scoped effects that stack or need restoration require a separate contract.

## Acceptance

One fixture changes current health on an NPC and confirms an identical NPC is unaffected. Two callers can update a live NPC in queue order. Normal damage still applies. A despawned NPC fails cleanly on a subsequent edit.
