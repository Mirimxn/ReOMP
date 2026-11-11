# Minifig Part Viewer

Single-page 3D viewer for the Skywalker Saga minifig body (SUPER_MINIFIG_DX11.GHG, highest-detail pieces) that loads
game textures (`.texture`, BC1/BC3/BC7 `.dds`, PNG) and models (`.gsc` rigid, `.ghg` skinned bind pose).

- `prep.py` extracts the body pieces and the 212th example (textures, helmet, kama, satchel) into `assets.js`.
- `tables.js` + `decode.js`: BC1/BC2/BC3/BC7 decoders (BC7 checked byte-for-byte against Pillow) and the game `.texture` reader (RTXT block, mip data at the end of the file).
- `model.js`: HSEM reader; keeps textured lists (skinned lists only for GHG), merges detail levels by matching bounds.
- The page is `head.html` + `data_block.html` (placeholders filled with the three scripts and `assets.js`) + `app_script.js`; plain WebGL, no libraries.
- Default heights: helmets 0.3742 (clone prefab hat height), pauldron 0.2891 (Neck_Locator), skirts/kamas 0.104, other skinned models 0.
