# `.prefab_baked` / `.scene_baked` format (character prefabs)

Notes from `CaribDiss.prefab_baked`, `CLONETROOPER.prefab_baked` (Typho-based), `Finn.prefab_baked` and `CloneTrooper_Phase2_Utapau.prefab_baked` (Windows Steam, Nov 2026).

## Header (big-endian)

- u32 header chunk size, then `.CC4HSERHSER`, version, self path (u16 length incl. `\0`), 16-byte self ID.
- Dependency string table (folders and file names, `\0\0`-separated, padded to 4) and dependency records (folder index, name index, ID). Character prefabs list several hundred dependencies, so the header is 35–38 KB.
- The source path `=chars/minifig/<folder>/<name>.prefab` is followed by the **resource ID**, which the roster's `Graph` column must carry.

## Body (little-endian, self-describing)

Every value is a record: `u32 size (incl. itself) | u32 name length | name\0 | payload`.

1. `StreamInfo`, `ResourceTimeStampGUID` (16 bytes), `GraphType`, `BakeContributingResources` (count, then `guid, guid, u32 len, path\0` per entry).
2. A section holding:
   - **`TypeList`**: basic types, each `Type` = string name + u32 byte size (`Char` 1, `Short` 2, `Int` 4, `Int64` 8, `Float` 4, `String` -1, `Colour3` 12, `NuHSpecial` 16, `Vec3` 12, `Vec4` 16, `Mtx` 64, `Ptr` 8, `ClassObject` 56, `ClassObjectRef` 56, `Half` 2, `Data` -1, ...).
   - **`ClassList`**: u32 class count (960 in CaribDiss), then `Class` records: class name string (`apiEntity`, ...), `Version` (float), `Types`/field definitions (`Name`, `Mobility`, `Platform`, `NameSearch`, `Components`, ...) with type index and offsets.
3. `u32 1`, then **`OLST`**: the object list. Objects are nested `MOBJ` records: `00 00 ff ff 01`, a 16-byte GUID, then named sub-records (`RootNode`, ...) and child `MOBJ`s. The root `MOBJ` covers almost the whole list.
4. A trailing table of u32 values (`ff ff ff ff`, small indices, ~2.5 KB): probably object or link indices.

## Character parts seen

- Body: `super_character/super_minifig/super_minifig_dx11.ghg`, textures in `super_character_texture/minifig/` (head, body, left/right arm, left/right hand, left/right leg, hips; `_nrm`/`_brdf`) and per-character `diff`, weather in `super_character_texture/weather/super_minifig/`.
- Face: `super_face`, `face_<name>.prefab`, brow/eye/upper/pupil/mouth/teeth/tongue textures.
- Attachments, each a sub-prefab plus model and textures: `hat`, `facetex`, `additionalmodel`, `skinnedskirt` (capes), `torso_attachment`. Hat models carry a part name (`HAT_CloneTrooper`, `Hat_CloneTrooperEpisodeIII`, ...).
- Class: `templatecharacters/classtemplates/<class>.prefab` (`bountyhunter`, `resistancerebelsoldier`, ...). Abilities: `minifig/<name>/abilities/<name>_abilities.prefab`. Character data: `minifig/<name>/character/<name>_character.prefab`.
- Items and weapons: `items/*.prefab`, `weapons/*`, `item_graphs/templates/*`, with item models (`blasterrifle_dx11.gsc`, ...).
