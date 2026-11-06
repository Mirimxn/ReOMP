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

## Decoding progress (6 Nov 2026)

Reader: `tools/character-maker-prototype/reader/` (`schema.py` parses `TypeList` and `ClassList`; `decode.py` walks objects).

- **Types** (26 in CaribDiss): `Char`, `Short`, `Int`, `Int64`, `Float`, `String`, `Colour3`, `NuHSpecial`, `Vec3`, `Vec4`, `Mtx`, `Ptr`, `ClassObject`, `ClassObjectRef`, `Half`, `Data`, (unnamed 16), `HalfVec3`, `UChar`, `Colour4`, (unnamed 20), `NuTransform` (48), `NuGuid` (16), 23 (resource reference), `NuQuat`, `Colour32`.
- **Class** record: name string, `Version` (float), `Types` = `u8 0`, `u32 count`, then per field `u32 type, string name, i32 offset, i32 a, u32 flags, i32 b, i32 c`.
  - Base class: flags `0xC0000000`, offset 0, `type` = class index of the base. Its fields come first.
  - `c == 0`: list (u32 count, then items).
  - Flags `0x9xxxxxxx`: runtime-only, not stored (`Parent Transform`, `Target Transform`, `GameFlags`).
  - Type number >= 26: embedded instance of class `type` (e.g. `Model Resource` -> `nttResourceStrongAutoLoadHandleT<NuScene>`). Some of these use their own layout (resource handles store `u32 len + path`).
- **Object** (`MOBJ` record): `u32 prefix` (`00000000`; entities `0000feff`, root `0000ffff`), then the class fields.
  - `apiComponent`: `Enabled` (u8), `Guid` (16), `ManagedByAsset` (u8), `Locked` (u8).
  - `apiEntity`: + `Name` (string), `Mobility` (u32), `Platform` (u16), `NameSearch` (u8), `Components` (list of `u8 present, u32 class index, MOBJ`).
  - `RootNode` holds the root entity's fields from `Mobility` on, with 92 components in CaribDiss.
- **Resource reference** (type 23): `u32 ref type, u8 has id, [16-byte id], u32 len, path\0`.
- **Attachment entities** seen in CaribDiss: `SkinnedSkirt_Fabric` (10 components), `AdditionalModel_CaribDiss` (6), `FaceTex_CaribDiss` (4).
- Attachments find their place at load time through `AttachToLocatorComponent.Locator` (a joint name); the transform links are runtime-only. Open question: the trailing u32 table (636 entries in CaribDiss) may hold the transform parent indices.

## Counters and the link table (6 Nov 2026)

- The two u32 values right before the `TypeList` record are **the total object count** (root included: CaribDiss 2446, CLONETROOPER 2392, the Airborne Trooper copy 2481) and **the link table length** (636, 597, 636).
- The u32 table after `OLST` is the **link table**: each slot is an object number (pre-order position of the `MOBJ` in the tree, root = 0) or -1. Max value 2425 in CaribDiss, 2446 objects.
- Link fields are the fields flagged `0x8...` or `0x9...` (not the base-class marker). Their `type` is the **class they point at**, not a basic type: `Locator entity` -> class 0 `apiEntity`, `Parent Transform` -> class 2 `apiTransformComponent`.
- Slots run in object order: CaribDiss objects 1–15 own slots 0–7, and the cape (`SkinnedSkirt_Fabric`, objects 16–41) starts at slot 8 (pointing at its own transform, object 17).
- The cape's slots also point outside it: object 66 (the body `SkeletonComponent`) and object 118 (`CharacterLodComponent`). Copying an attachment means remapping these to the receiving character's equivalents.
- Counting link fields per class gives 437 slots instead of 636: the rest sit inside lists and embedded structures. The exact slot order needs a decoder that reads every field.

## Copying an attachment: what has to change in the receiving file

1. Append the entity block (`01, u32 class, MOBJ`) to `RootNode`'s component list (count + 1), as the last objects so existing object numbers stay the same.
2. Translate the class numbers inside the block to the receiver's class list; add missing class definitions (the cape needs 7: `AttachmentOffsetComponent`, `BoneModifierJointToJoint`, `BoneModifiersComponent`, `Hair`, `MatchJointsComponent`, `MatchJointsData`, `ParentResultNode`), translating the class numbers inside them too.
3. Grow the sizes of `RootNode`, the root `MOBJ`, `OLST` and `ClassList`; update the object count and the link-table length.
4. Append the attachment's link slots, with outside links remapped (body skeleton, LOD component, locators).
5. Possibly add its files to the header dependency tree (a folder/file name tree: 319 names, 436 nodes of `u16 prev sibling, u32 name offset, u16 parent, u16 file, u16 ?` in the Airborne Trooper). To be tested; the game may load files that already exist in its archives without it.

## Full decoder status (6 Nov 2026, `reader/dec2.py`)

94% of CaribDiss objects (2292 of 2445, everything nested inside included) decode to their exact size. The cape decodes 24 of its 26 objects.

Field rules found so far:
- Base class (flags `0xC...`, offset 0): its fields come first, recursively.
- `c == 0`: list, u32 count then items.
- Flags `0x9...`: link to an object, stored in the link table, nothing inline. **Exception (in progress):** links to classes that are not components (e.g. `AnimBlendTreeComponent.Root Node` -> `ResultNode`) are stored inline as 8 zero bytes, an 8-byte name hash and the node name string, followed by 9 more bytes still to be explained.
- Flags `0x8...`, offset -1, or pointing at a component/entity class: not stored inline (link or runtime).
- Flags `0x8...` to `nttResource...Handle...` / `...ResourceHandle` classes: resource link `u32 type, u8 has, [16-byte id, u32 len, path\0]`; nothing after `has` when it is 0.
- Flags `0x8...` to other classes: the class's fields inline (an embedded struct).
- `ClassObject` / `ClassObjectRef`: `u8 present`, then `u32 class` and the `MOBJ` record.
- Fixed sizes from `TypeList`; `String` and `Data` are u32 length + bytes.

Still failing: `BuildItTimeline*`, `RunBTAction`, `EntitySelectorViaScorer`, `InventoryItemLocator`, `ChooseItContext2`, `GameFlagComponent`, `ProjectileComponent`, `MiniCut`, `ActionMapComponent`, `RangedAttack`, `AnimBlendTreeComponent`, `PlaceResourceSequence`.
