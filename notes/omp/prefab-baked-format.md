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

## Adding an attachment (6 Nov 2026, `reader/add_attachment.py`, in-game test pending)

- The root object is an `apiEntity` named `RootNode`; its `Components` list runs to the end of the root `MOBJ`. Top-level entities (helmet, cape, locators, ragdoll, ...) are items of that list.
- Inline animation-node links (`0x9...` fields pointing at `AnimNode`-derived classes, e.g. `Root Node`): 8 bytes, 8-byte name hash, name string, 8 bytes.
- **Link slots** are taken, in object order, by `0x9...` links and by `0x8...` links to components or entities. CaribDiss objects 1–15 take exactly 8, and the cape takes slots 8–29 (22).
- Body layout is flat: `StreamInfo`, 8 bytes, `ResourceTimeStampGUID`, `GraphType`, `BakeContributingResources`, `u32 object count, u32 link count`, `TypeList`, `ClassList`, `u32 1`, `OLST`, link table. No size encloses `ClassList` or `OLST`.
- Copy steps: append the entity block at the end of the root's components (so no existing object is renumbered); translate class numbers (`01, u32 class, MOBJ`) and add missing class definitions with their class numbers translated (type numbers are class numbers for base fields, `0x8/0x9` fields and types >= 26); append the entity's link slots with inside links renumbered and outside links mapped to the same class at the same place (e.g. root `SkeletonComponent`, root `CharacterLodComponent`); grow the root `MOBJ`, `OLST` and `ClassList` sizes and the class, object, link and component counts.
- First test: CaribDiss `SkinnedSkirt_Fabric` (kama) added to the Airborne Trooper copy: 26 objects, 10 classes, 22 link slots, +9,704 bytes. The header dependency tree was left unchanged (the kama's files are in the game archives).

## Header dependency list (6 Nov 2026, `reader/deps.py`)

The game only loads files that are on this list: the kama's objects without its files on the list spawned nothing.

- After the self ID: `u32 1, u32 2, u32 file count, u32 node count, u32 name block length`, the name block (names separated by `\0\0`), then the BrickVault-style **name tree**: 12-byte nodes `u16 last child, u16 previous sibling, i32 name offset, u16 parent, u16 file number` (node 0 = root, directories have file number 0).
- Then `u32 0, u32 0, u32 record count` and **64-byte records**: `u32 kind` (1 `gsc`, 2 `ghg`, 36 texture, 44 anim, 47 bt, 53 audio_event, 62 prefab_baked, ...), `u32 tree node`, `u32 0`, `u32 platform flags` (`0x4fffffff` the `_dx11` model, `0x8fffffff` the `_nxg` model, else `0xffffffff`), `u32 0, u32 0, u32 -1`, 16-byte ID, `u32 link type` (2 or 1), 16 zero bytes.
- Then build info and the source path with the resource ID, up to the header end. The header chunk size (BE u32 at 0) covers everything.
- Adding a file: add missing folders and the file to the tree (new node: previous sibling = parent's last child, then parent's last child = new node; files get the next file number), append its record with the node number, and update the counts and the header size.

## Template approach and findings (7 Nov 2026)

- **Added entities are ignored.** Neither the CaribDiss kama nor the CaribDiss helmet copied into the Airborne Trooper (with classes, links and load list entries) was ever loaded by the game. Something not yet found decides which entities are built. Until then, custom characters start from a template that already has the attachments they need.
- `CT212Kama` (`reader/make_ct212.py`) is a CaribDiss copy: kama kept, `AdditionalModel_CaribDiss` and `FaceTex_CaribDiss` switched off (`Enabled` = 0 on the entity and its components), airborne helmet model (keeping CaribDiss's helmet ID) with part name `Hat_Cl2ab`, helmet lowered to the Utapau trooper's height (local and world `y` 0.3742), 212th body, leg, arm and hip colour textures.
- **Material slots** in `nttRenderTTExportObject.Material State Overrides`: `diffuse_0..3`, `normal_0..3`, `specular_0..3`, `detail_0..3`, `mask_3`, each a resource handle. The helmet's colour texture goes in `diffuse_1` of the second override; an empty slot is a 5-byte handle and can be filled. Size-changing edits grow every enclosing `MOBJ` and `OLST`.
- **Textures:** `.texture` = header chunk (self path, ID) + `.CC4HSERRTXT` chunk (`u32 0x13, width, height, depth, mips, ?, format` (31 = BC7), inner 16-byte ID at RTXT+46) + all mip data. A BC7 DDS (DX10, DXGI 98) converts by copying its data after the DDS header. Copies need new self IDs **and** new inner IDs.

## Rigid attachment from an FBX (pauldron test)

- TSS GSC mesh layout (from Hologram's `MESHC9`/`DXTVA9`): `HSEM` v0xC9, part count, then per part `u32 1`, `SMNR` v0xC9, vertex lists (`u32 0x502, u32 count, DXTV v0xA9, defs (var, type, offset), 6 zero bytes, data, u32 0`, or a `0xC000xxxx` reference + 8 bytes), `u32 0`, an index list (`u32 2, count, 2, u16 data`, or a reference + 4 bytes), then the part info: `ioff, icnt, voff, u16 0, vcnt`, ... and a bounding box (centre xyz, `1.0`, half-extents xyz). Only one size field: the chunk size at `header size + 4`.
- Hat GSCs have 8 parts (4 LODs x a full list and a position/normal list) sharing one index list. Vertex: position `vec4half`, normal `vec4mini` ((n+1)*128), colour `ffffffff`, tangent 0, uv `vec4half` (u,v,u,v), uvSet2 16 zero bytes. Counter-clockwise winding with the normal.
- The model's internal name sits in the fixed-size NTBL after `default_string`; the prefab's Special Name must match it.
- The user's Blender scene is at 100x game size: FBX cm / 10000 = game units (the neck locator is at y 0.2891 and the FBX object at 2900 cm).
- The FaceTex slot (rigid GSC on `AttachToLocatorComponent`, locator name string) can host a rigid model: change `Face_Locator` to another skeleton locator (`Neck_Locator`, `Spine_Locator`, `Backpack_Locator`), disable its AnimBlendTree, swap the model, the Special Name and the material overrides.
- A GSC's header names its folder (`chars/super_character/hat/`) and the game looks for its `.pc_shaders` and `.nxg_textures` there, whatever folder the GSC itself is served from. A GSC copied from a hat must stay in the hat folder, or it never loads (the log shows thousands of requests for it).
- Two characters with the same `previewBase` share one preview slot, and the first one wins. Give each character its own base.
- Plastic colours: each `nttRenderTTExportObject` has `Material State Overrides` items (u16 `ID` = material index in the model) with `colour_tint_enabled_0` and `colour_tint_0` (linear RGB floats). On `super_minifig` the clone trooper uses IDs 1-2 skin (0.776, 0.435, 0.29), 7-8 black (0.067), 3-6 and 9-11 white (0.957). Hats and skirts use item 1.
- Torso-attachment slot test: with the GSC in the right folder its shader and texture lists load, but the pauldron still does not show. That slot (skeleton + AttachmentOffset on the `Neck` joint) does not draw a rigid GSC. Next try: the face slot on `Neck_Locator`.
- A diffuse override in item 1 of the kama (`skinnedskirt_fabric_kamacloth`, materials TT0_Skirt_FabricKamaCloth and TT1_Breakup) loads the texture but does not show it. Next try: diffuse_0-2 on both items.
- A GSC stores its model name twice: in the NTBL after `default_string`, and again near the end of the file (`$` + name). Rename both when copying a model, since the prefab's Special Name matches one of them.
- Texture overrides (diffuse/normal/specular) only reach the parts named by the render object's Special Name; tints (`Material State Overrides`) apply regardless. Caribdiss's kama has an empty Special Name, so its texture overrides did nothing: use `[All Specials]`.
- Torso-attachment slot, retested with the model name fixed: the rigid pauldron still does not show there. Use the face slot.
- `.NXG_TEXTURES`: a header chunk, a `TSXT` chunk listing the textures (id + `.nut` name each), then the embedded textures as plain DDS files (DXT1/DXT5, NVTT header) back to back; sizes come from each DDS header. Character templates embed tiny textures (8x8 `lego_white255`, `lego_black`, `dummy_normal`, roughness). The kama's material reads only these, so to texture it: copy the GHG, PC_SHADERS and NXG_TEXTURES under a same-length name (`skinnedskirt_fabric_kamact212`), embed the texture as DXT1 with mips in place of `lego_white255`, and point the prefab's kama handles and load-list records at the copy (keep the original handle ids).
- Test result: the kama copy with the texture embedded in place of `lego_white255` loads but still draws white. Likely the game caches embedded textures by entry id or name (`lego_white255` is shared by every character), so the copy's entry now gets a new id and name. The pauldron was drawn "broken": the FBX is a thin shell meant to be seen from both sides and the game culls back faces, so the builder now adds the inside faces too.
- The kama GHG's material names each texture by the NXG entry id and `.nut` name (`b2cef04c...`, `template/lego_white255.nut`), so the copy must change both in the GHG and in its NXG for the embedded texture to be used.
- Sounds: `nttAudio::ComponentEmitter` entries are keyed by an `ID` string (`VO_Agree`, ...) with one `Event` handle each; copy a donor's voice by matching emitters on path and entry IDs (`donorswap.py`). The body's animation set is the root `AnimBlendTreeComponent`'s `*_animation.pod_baked` handle. Add the donor's load-list records for every new path.
- Roster group: `CharacterClass` column (clone troopers 2, Caribdiss 3); set it with `columnOverrides`.
- Test result: with the kama material pointing at its own embedded texture (new id and name) the kama is still white. In each NXG entry, the name is followed by a u32 and a kind byte. Templates use `04` (`03` for the dummy normal); the gunship's real albedo, decals and normal use `0b`. Next try: kind `0b` on the kama's texture entry. If that also fails, the kama's cloth shader probably does not sample a colour texture at all.
- Kama material, test result: an embedded texture with kind `0b` is still drawn white. The kama material has only the base plastic layer (`lego_white255` once). The printable models have a second layer (`lego_white255` twice: hat, Palpatine cape, Leia's Ewok Village skinned skirts), and the prefab's `diffuse_1` fills it.
- The cape material needs a `uvSet2` vertex field the kama lacks. Leia's skinned skirt (`SKINNEDSKIRT_FABRIC_LEIA_EWOK_VILLAGE_INGAME`) has exactly the kama's vertex layout (skinned and posed lists) and LTMU v0x180 with 2 materials. The kama has v0x17e with 4 (plus shadow lists).
- GHG chunk map: header, `02UN`/`OFNI`/`LBTN`, `HSEM` meshes, `LTMU` materials (each record ends with its DXTV vertex layout; the texture-reference block follows record 1), `SUPC`, `PSID` (draw groups + specials, `[All Specials]` matches these), `BNAT`, `3ALA`, `BCSB`, `BCCO`, `5LVI`, `LOGH` (skeleton per LOD), `ATEM`. Only one size field: the chunk size after the header.
- Experiment: kama GHG copy with Leia's whole LTMU section (2 materials) + Leia's PC_SHADERS and NXG_TEXTURES (self paths renamed), print on `diffuse_1`. Untested: whether the kama's shadow parts break by referring to materials 3/4.
- Leia-material test 1 crashed the game. Likely cause: GHG/GSC files number their objects in order (mesh vertex and index lists, then each material's DXTV), and later data refers back to them as `c0 00 xx xx`. Leia's materials referred to her objects 9 and 10, which in the kama file are mesh lists. Test 2 renumbers them to the kama's 27 and 28. The kama's own material section refers to 27 to 30 (4 DXTVs).
- Leia-material test 2 (references renumbered) also crashed. New approach (`ghg_mesh_swap.py`): Leia's skirt file is the host and only its mesh is replaced with the kama's LOD0 (skinned + posed lists, shared index list, part infos). Her materials, skeleton (`LOGH`, same bone names as the kama), draw lists and her shader/texture files from the game stay as they are, so the internal numbering stays consistent. SUPC/BNAT/3ALA/BCSB/BCCO are identical between the two files; `5LVI` holds the LOD count (kama 4, Leia 1).

- A GHG/GSC header stores its own path with an exact BE u16 length (incl. NUL) followed directly by the 16-byte id. Writing a shorter path into the old field and padding it with zeros makes the game ask for the file again and again (25k+ requests in the log) and the model never appears. Rebuild the header with the new length and update the header size. (Fixing this alone did not make the Leia-host kama load: build 18 was still requested 119k times, so the mesh swap itself is rejected and the endless reloading also starved other loads such as voices and the 501st helmet.)
- Old-format GHGs (`//ntt/dev` header, LTMU v0x17e) store the body-size field as len - header - 8 - 128; newer ones (`//paddington/dev`, v0x180) as len - header - 8. v0x17e and v0x180 material records differ in layout, so records can't be moved between them.
- `SKIRT_ATTACHMENT_DX11.GHG` is old-format, skinned, and has `uvSet2` (float4 = uv0,uv0) plus a printable second layer. Build 19 gives the kama its front and shadow materials (`mesh/kama_skirtmat.py`), adds `uvSet2` to the kama's textured lists, and ships the skirt's PC_SHADERS/NXG_TEXTURES under the kama's names.
