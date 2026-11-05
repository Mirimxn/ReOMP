# In-game UI

Provide a shared host for mod interfaces, including the single input configuration screen. [Overlay1](../../mod-sources/overlay1/mod.md) already supplies an off-screen Chromium view over DX11 and handles keyboard and mouse capture.

## Implementation sequence

1. Reuse the existing DX11 and CEF renderer from `overlay1` for the first release. Keep one shared render host and verify startup, resize, focus changes, scene transitions and shutdown. Use the matching native CEF files from the [known-working package baseline](../../scripts/package-release.ps1).
2. Let mods register and remove panels in the shared menu, keyed by their mod ID and panel ID. The host owns focus, cursor and capture. Route player actions through [input](input.md) and keep CEF-specific APIs behind the host.
3. Host the single input screen using the input provider's action catalogue and binding operations. The manager implements its own desktop view over the same contract.

## Acceptance

Two consumer contributions coexist. Opening, resizing and closing the UI preserves game rendering and restores input and cursor state. The input screen edits a binding visible to the desktop editor.
