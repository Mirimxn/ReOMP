# Rigid pauldron

Built from `rigid_pauldron.fbx` with `tools/character-maker-prototype/mesh/make_pauldron_gsc.py`, inside a copy of the CL2AB helmet GSC (so the files live in `chars/super_character/hat/`).

| Folder | Model name | Shoulder |
| --- | --- | --- |
| `right/` | `PauldronR` | Right (negative x, as placed in the FBX) |
| `left/` | `PauldronL` | Left (mirrored) |

Both are modelled in `Neck_Locator` space. In a prefab, set the slot's Special Name to the model name (or `[All Specials]`). The colour texture uses FBX UV layer 1 (`UVMap_textured`); the normal map uses UV layer 0 (`UVMap`). `textures/` holds the BC7 game textures made from `pauldron_diff.png` and `pauldron_nrm.png`.

The face-texture slot on `Neck_Locator` draws the pauldron.
