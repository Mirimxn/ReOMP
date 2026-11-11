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
