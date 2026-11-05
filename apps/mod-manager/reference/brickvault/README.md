# BrickVault archive reader reference

The donor checkout at `D:\BrickVault-jubjub` identifies its branch as `codex/v13-in-memory-projfs`. The files below cover the Skywalker Saga archive reader, file tree, extraction and format notes. Use `local/` for private reference copies when the donor checkout is available. No redistribution licence was found in the donor or upstream. The release implementation uses its own native Rust reader and can start from the format notes below. [BrickVault upstream](https://github.com/connorh315/BrickVault)

| Donor path relative to the checkout | What to use it for |
| --- | --- |
| `dat_file_format_complete_specification.md` | PC archive layout and byte offsets |
| `BrickVault/Types/DATFile.cs` | Trailer lookup and compression chunk framing |
| `BrickVault/Types/DAT_v13.cs` | v13 tree and file tables, including the minor-version offset change |
| `BrickVault/FileTree.cs`, `FileTreeNode.cs`, `ArchiveFile.cs`, `NewArchiveFile.cs` | Logical paths and archive entry metadata |
| `BrickVault/RawFile.cs` | Integer and string reads |
| `BrickVault/Decompress.cs`, `BrickVault/Decompressors/{RC4,DFLT,LZ2K}.cs` | Codec dispatch and how individual chunk payloads are decoded |

## PC DAT v13 format for the manager

- The leading little-endian signed 32-bit locator gives the embedded trailer offset directly when positive. A negative locator encodes the offset in 256-byte units. Use 64-bit checked arithmetic. Validate the candidate against the archive bounds, `4CC.`, `DAT0` and big-endian version `-13`. Check both the direct location and a candidate one 32-bit address window higher when the archive spans that boundary. Treat multiple valid candidates as an error. A valid wrapper and table bounds take precedence over the second leading word, which is inconsistent across donor fixtures.
- At trailer byte 16, read four big-endian 32-bit values for minor version, file count, tree node count and name block length. The NUL-terminated name block starts at byte 32. After it, each 12-byte node holds its last child and previous sibling indices, a signed name offset, parent index and file index. A name offset of `-1` has no string. Reconstruct a file's logical path through parent links. Validate names and links against their own table bounds and reject cycles.
- Skip the three 32-bit words between the node table and file table. Minor versions 0 and 1 use a 32-bit offset. Version 2 uses a 64-bit value with the codec flag in the high byte. Each entry ends with two big-endian 32-bit sizes, stored and decoded. Validate each entry range against the data region and retain the archive identity alongside the logical path.
- Raw entries have equal sizes and no compression flag. Other entries use 12-byte chunk headers with a four-byte tag and two little-endian size fields. `LZ2K` reverses the size order compared with `OODL`. `ZIPX` uses RC4 with the little-endian stored chunk size as the key, even if the sizes match. Other equal-size chunks can carry raw bytes. Bound all reads and verify the output size per chunk and per entry.

Validate these format rules against synthetic fixtures and real archives as described in the [Mod Manager plan](../../../../plans/mod-manager.md#game-assets-and-replacement-mods).
