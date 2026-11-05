# Character fixtures

`test-trooper` adds `OMP_Test_Trooper` ("OMP Test Trooper") to the `Characters` table as a copy of `CloneTrooper_Phase2_Utapau`, using the game's own prefab, so it needs no packaged files.

Verified on Windows Steam (6 Nov 2026): the entry appears in the character roster as its own character next to the original, unlocked and free, and is playable. Icon support for characters was added afterwards (fixture now sets `icon`); in-game check pending.

## Characters table

1103 vanilla rows, 34 columns. The ones the action sets: 0 `Collect State` (2 = unlocked), 1 `Graph` (prefab), 3 `IconFileName` (sprite name, e.g. `clonetrooper_phase2_utapau.png`), 4 `DisplayStringID`, 5 `DescriptionStringID`, 7 `BaseCharacter`, 8 `CurrentlySelectedOutfit`, 9 `Price`, 10 `CharacterBaseNameID`, 30 `Cheap Graph` (roster preview). Custom-data columns 13, 14, 18, 21, 22, 24 and 33 are not copied.

Character icons are one sheet: `ui/icons/characters/characters.tsh` with `characters_dx11.texture`, the same layout as the vehicle sheet.
