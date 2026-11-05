# Animations

Let mods request supported animations on a live character. Use the same entity and world lifetime rules as [game access](game-access.md).

## Implementation sequence

1. Compare the inherited NPC respawn animation call in [Npc.cs](../../mod-sources/galaxy-unleashed/galaxy-unleashed-runtime/src/Npc.cs) with the player playback paths in `D:\TSS Emotes\tss-emotes\src\EmotePlayback.cs`, `EmoteScriptAnimationCommandPlayback.cs` and `EmotePlayAnimationCommandDataPlayback.cs`. Verify the applicable game bindings. The emote code has paths gated on Steam-specific helpers.
2. Start with single existing game clips on live player characters and NPCs. Verify each actor path separately. Keep animation lookup and playback state separate from emote definitions, shortcuts and UI. If the adapter has no verified path for a variant or actor, return an unavailable result.
3. Return whether the game accepted the request. Expose completion and interruption where the adapter can observe them. Release resources when playback ends or the target disappears. Treat scene transitions, character changes and invalid clip references as explicit outcomes.

## Acceptance

A fixture plays a known clip on a player and an NPC, then returns control to the game. Verify an invalid clip and an actor or world change during playback. Record each actor and game variant actually tested.
