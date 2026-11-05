# Input

Give mods rebindable player actions through one API. The single in-game input settings screen and the [Mod Manager](../mod-manager.md) edit the same bindings. [InputHook1](../../mod-sources/input-hook1/mod.md) supplies a starting point for game-window messages.

## Implementation sequence

1. Use a manifest action to declare a stable action ID scoped to its mod, a display label and a default keyboard binding. The manager reads the declaration while the game is closed. Game code subscribes to the declared ID. Keep IDs stable across package updates.
2. Adapt window capture for keyboard press and release, optional modifiers and focus changes. Send a press once until the key is released. Give the UI an input-capture mode so rebinding, text entry and menu navigation consume events before gameplay actions see them. Keep capture and dispatch behind the provider API so controller support can be added independently.
3. Store binding overrides in one manager-owned data directory shared by both editors, separate from save-scoped SQLite data. Store one versioned JSON record per action, keyed by its mod ID and action ID. Reset removes the override. Write through a temporary file and replace the record atomically so edits to different actions are independent. If both editors save the same action, the last completed write supplies the value. Refresh the editor on open and after changes. Deploy the binding-directory path in manager-owned runtime configuration, using the Windows path visible to the game, including the Proton mapping on Linux. Validate the path from both sides before enabling writes.
4. Expose the action list and binding operations to the [UI provider](ui.md). The UI hosts the in-game screen and consumes input. Keep the input service independent of the UI so the two packages can load in dependency order.

## Acceptance

Two fixture mods declare actions. Both editors show the same entries and preserve each other's changes. Rebinding changes what fires, including after restarting the game. Opening a menu or capturing a binding does not fire the gameplay action it consumes.
