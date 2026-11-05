# NPC orders

Give feature mods a shared API to move an NPC to a point, follow an entity or attack a specified opponent. Use [game access](game-access.md) for the update thread and entity lifetime. Accept eligible existing NPCs and entities returned by [NPC spawning](npc-spawning.md). Feature mods can compose movement orders into patrols and use combat orders to implement their own encounter rules.

## Implementation sequence

1. Extract the navigation operation from Galaxy Unleashed's `UpdateMoveToBattleFlagBehavior`. It sets a `NavRoute` goal, reads the route heading and sends movement to `HorizontalCharacterMover`. Verify which NPCs support this path, who owns the native route and goal, and how to stop issuing movement so normal AI resumes. Reproduce one move-to-point order on a flat, navigable map before adding follow. [Donor NPC code](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/Npc.cs)
2. Implement follow using the same routing operation with a live target and a requested stopping distance. Recalculate when the destination moves beyond the route tolerance. Stop on cancellation, despawn or world change. Report arrival or no route for a point order and report target loss for a follow order.
3. Trace a normal NPC engagement to find the native target assignment and release operations. Start from `OpponentChooser` and `OpponentSelectorComponent`. The donor checks for a naturally selected opponent. Prove explicit target selection and cleanup before enabling attack orders for a game variant. Return an unavailable result if the binding or NPC capability is missing.
4. Give each NPC one active provider order. The submitting mod receives an owner-scoped handle and can cancel or replace its own order. If another mod already owns the order, reject the request with the owner identified. Release route and target state on cancellation or invalidation. Cleanup releases only state the provider still owns.

## Acceptance

On Windows, move an NPC to a point, follow a moving target, and verify a targeted attack through the native combat path. Exercise an unreachable point, target despawn and scene exit. Run two fixture mods that order different NPCs, then have them request orders for the same NPC. Verify that rejecting the second request leaves the first intact and that cancelling the first returns the NPC to normal behaviour. Record which operations and variants were verified.
