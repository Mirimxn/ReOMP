# Character registration

Provide a manifest action for registering a playable character and a roster lookup for mods that need character IDs. Start from [the v1 character action](../../mod-sources/v1/v1/src/ModRegisterCustomCharacterAction.cs) and [roster lookup](../../mod-sources/v1/v1/src/FetchCharactersInfo.cs).

## Implementation sequence

1. Map the inherited registration fields to what the game requires to add a roster entry. Identify the game initialisation point for applying declarations. Collect registrations during manifest loading, then apply them once the target tables are available.
2. Validate character and template IDs. Report duplicate IDs with both owners. Reuse [asset replacement](asset-replacement.md) for packaged resources and document the declaration order and required asset references.
3. Expose stable character lookup for consumers such as [NPC spawning](npc-spawning.md). Keep character selection and player-control behaviour in their respective gameplay systems.

## Acceptance

A fixture adds one character based on a verified template. The character appears in the roster, can be selected and resolves through the public lookup. Exercise a missing template and two packages claiming the same ID.
