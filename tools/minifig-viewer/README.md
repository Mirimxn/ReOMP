# Minifig Part Viewer

Single-page 3D viewer for the Skywalker Saga minifig body (SUPER_MINIFIG_DX11.GHG, highest-detail pieces) that loads
game textures (`.texture`, BC1/BC3/BC7 `.dds`, PNG) and models (`.gsc` rigid, `.ghg` skinned bind pose).

- `prep.py` extracts the body pieces and the 212th example (textures, helmet, kama, satchel) into `assets.js`.
- `tables.js` + `decode.js`: BC1/BC2/BC3/BC7 decoders (BC7 checked byte-for-byte against Pillow) and the game `.texture` reader (RTXT block, mip data at the end of the file).
- `model.js`: HSEM reader; keeps textured lists (skinned lists only for GHG), merges detail levels by matching bounds.
- The page is `head.html` + `data_block.html` (placeholders filled with the three scripts and `assets.js`) + `app_script.js`; plain WebGL, no libraries.
- Default heights: helmets 0.3742 (clone prefab hat height), pauldron 0.2891 (Neck_Locator), skirts/kamas 0.104, other skinned models 0.
- Materials hold four maps (colour, normal, BRDF, emission), each with its own UV set (UV 1 = first pair of uvSet01, UV 2 = second pair, the print layer) and a vertical flip. File-name suffixes `_NRM`, `_BRDF`, `_EMISSIVE` pick the map.
- BRDF is read as R = specular strength, G = roughness (game BRDF maps: plastic helmet G≈8, fabric G≈52-67); B is not used. Normal maps default to DirectX green (Y down).

## Character prefabs and mod export

- `prefab.js` (`PF`): JavaScript port of `tools/character-maker-prototype/reader` (schema, object table, field decoder, header load list) plus the edits `make_ct212p.py` made: texture handles, tints, model swaps (all handles of the old model inside the entity), Special Name, entity disable, rigid part position, own name/ids, roster link `OMP_<ID>`, header records for new textures and models. Edits grow every enclosing `MOBJ` and `OLST`.
- Loading a `.prefab_baked` makes it the base: abilities, voice and behaviour come from it. Body materials map by override ID (`BODY_IDS`); its attachment entities become parts. Dropped game files whose names match the prefab's references are shown and not shipped.
- The internal name keeps the base name's length (the header name tree and inner paths are patched in place).
- Export: textures are retargeted (`.texture`), wrapped (BC7 `.dds`) or encoded to BC7 mode 6 with mips (`tex_template.js` holds a game BC7 texture header); the ZIP holds one mod folder with `mod.json` (`v1/register-custom-resource-action` per file, then `v1/register-custom-vehicle-action` on the `Characters` table, `CharacterClass` override for the faction) and the preview scene when its `_cheapminifig.scene_baked` was loaded.
- Checked by re-reading exports with the Python decoder: same object count and decode results as the base, root and `OLST` sizes consistent, records added.
- `build.py` assembles `minifig-part-viewer.html`.

## Game folder and archives

- `gamedat.js` (`DAT`): reads TT `.DAT` archives in place with `File.slice` (port of DATExtract's new-format reader: header at the end, name tree, 64-bit FNV-1a path hashes mapping names to entries). Chunks: `DFLT` is raw deflate whose block types 0 and 2 are swapped (0 = dynamic, 2 = stored), checked against every entry of `CP_CLONEWARS.DAT` and `CP_1STPARTYPURCHASE.DAT`; `OODL` goes to ooz built to WebAssembly (`ooz-wasm/build.sh`, checked on ooz's Kraken/Mermaid/Selkie/Leviathan/LZNA/BitKnit test files; not yet checked on a `GAME*.DAT`).
- The game folder is opened with the folder picker (handle kept in IndexedDB, reopened with one click) or, where the page cannot use it, a folder file list. Later archives win; loose files win over archives.
- A character loaded with the game connected pulls its models, prints, normal/BRDF maps and preview scene from the game; the plain body gets the standard minifig normal/BRDF maps.
- Game texture format codes: 1 BC1, 6 BC3 (DXT5), 0x1f BC7.
- The view is mirrored in X: the game's coordinates are left-handed.
- UV defaults: a colour map from the print layer (`diffuse_1`) uses the first UV pair; one from the base layer (`diffuse_0`: torso attachments, pauldrons, face textures) the second; normal and BRDF maps the second.
- Head: Up is the 1 cm neck lift some characters carry (an `AttachmentOffsetComponent` on `Neck`); the export writes it when the base has that component.

## Skeletons and character bodies

- GHG skeleton (`LOGH`): u32 version 0x11, u32 0, u32 bone count; per bone u16 name length + name, a 64-byte matrix and 12 bytes (unused here), parent byte (0xff = root) and a flags byte; then u32 0, u32 count and the local bind matrices (big-endian, row-vector, translation in the last row). World = local x parent world. `skeleton.js` holds the standard minifig's (Head 0.2999, Neck 0.2891, Spine2 0.2428, Hips 0.1131: the same heights as the prefab locators).
- Skinned attachments carry bones named after the character's (`Head_Match`, `Spine2_match`, `Neck_match`, or plain `LeftArm`, `Hat`); the viewer places a part at (its bone's bind pose)^-1 x (the character's bone). Faces, capes, Yoda's head, Fode and Beed's torso and arms land on the body this way.
- A custom head (a `Head_match`/`Neck_match` model that is not `super_face`) hides the minifig head; a model with `LeftArm`/`RightArm` bones hides that arm and hand. This is inferred from the models; the prefabs carry no visibility flag for it.
- `super_minifig_customiser` and `super_small` bodies are drawn from their own model, each piece assigned to a body slot by where it sits against the body's bones. Material IDs 1-11 are the same as the standard minifig.
- UV defaults: skinned models (bodies, faces, capes, kamas) print on UV 1 and take normal/BRDF from UV 2; rigid models (helmets, hair, pauldrons) the other way round.
- Some prefabs store model paths with two trailing NULs; they are stripped when read.
- Not done: models with several materials (faces with eyes/brows/mouth textures) show one texture; that needs the draw groups in `PSID`. The astromech body is skinned bone by bone and still shows its pieces apart.
- Joint lifts: every `AttachmentOffsetComponent` (Joint + Offset) of an attachment moves that joint of the body while the attachment is shown: Fode and Beed's tail lifts `SpineBase` by 1.2 cm so it fits between hips and torso; CaribDiss's extra model lifts `Neck` by 1 cm. Body pieces and parts above the joint move with it.
- Open: which material a mesh part uses. Each LTMU record (with its `:VARIANT_AUTO` copies) carries a DXTV layout; vertex lists are shared between materials, so the material of a part has to come from the `PSID` draw list (entries end with `87 83 b3 00`; the last u16 is the part index).
