# Character maker prototype

Python scripts used to build the first working custom character (`OMP_Airborne_Trooper_212th`, a copy of `CloneTrooper_Phase2_Utapau` wearing a custom airborne helmet). Verified in game on 6 Nov 2026: own roster entry, own icon and preview, stays selected in the roster, helmet visible in the preview and in game, original trooper unchanged.

- `prefab_slots.py <prefab>` lists a character prefab's parts: models with their part names, and textures grouped by folder.
- `make_trooper.py` copies a prefab and its cheap scene under a new name and swaps the helmet model and textures.

## What a custom character copy needs

All edits keep every size the same: replacement names are the same length as the originals, or shorter and padded with zero bytes.

1. **Own self path and self ID** in the header of the prefab and the cheap scene.
2. **Own resource ID**: the 16 bytes after the source path (`=chars/minifig/<folder>/<name>.prefab`). If the copy keeps the template's, the game treats it as the template. The v1 module puts this ID in the roster row's `Graph` column.
3. **Roster link**: the prefab holds `Characters\0` + 16-byte entry ID + u32 length + entry name. Set the name to the new row (`OMP_<id>`, same length) and zero the ID, otherwise the roster jumps back to the template.
4. **Inner names**: rename `<name>.prefab`, `<name>_character.prefab` and `<name>_cheapminifig.scene` inside the copy.
5. **Swapping a part** (helmet, body textures, ...): change the name in the header dependency string table and in the body reference (`flag 01, 16-byte id, u32 length, path`).
   - Models keep the **original part's ID** (e.g. `df8de302...` for `hat_clonetrooper_phase2_dx11.gsc`) even when the path points at another file.
   - The **part name** right after the model path must be the new model's internal name (`Hat_Cl2ab`, `LOD_Group_1`, ...), padded with zeros.
   - Textures use the ID of the new texture file.
6. **New model files** need texture and shader files named after themselves: rename the `.shaders` and `_dx11.nxg_textures` entries in the model's header and ship matching `NXG_TEXTURES` and `PC_SHADERS` files with new self paths and IDs.
