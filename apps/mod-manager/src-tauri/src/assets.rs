//! The game file index behind the Game files view: every entry in the game
//! folder's `.DAT` archives plus loose asset files, browsed by folder or
//! searched, and extracted to a folder the player picks.
//! See plans/mod-manager.md#game-assets-and-replacement-mods.

use std::collections::BTreeSet;
use std::fs::{self, File};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::time::UNIX_EPOCH;

use serde::Serialize;

use crate::dat::{self, Archive, Codecs};

/// Where a file comes from: an archive by its index in `archives`, or loose.
pub const LOOSE: u32 = u32::MAX;

#[derive(Debug, Clone)]
pub struct Item {
    pub path: String,
    pub source: u32,
    pub entry: u32,
    pub size: u64,
}

pub struct AssetIndex {
    pub game_dir: PathBuf,
    pub archives: Vec<Archive>,
    pub archive_errors: Vec<String>,
    /// Sorted by path, then source; duplicate paths stay as separate items.
    pub items: Vec<Item>,
    fingerprint: Vec<(String, u64, u128)>,
}

/// Folders in the game folder that hold the manager's or OMP's own files.
fn skip_dir(name: &str) -> bool {
    let lower = name.to_ascii_lowercase();
    lower == "mods" || lower == "legacy-mods" || lower.starts_with("omp") || lower.starts_with('.') || lower.starts_with('_')
}

fn is_archive(path: &Path) -> bool {
    path.is_file() && path.extension().is_some_and(|e| e.eq_ignore_ascii_case("dat"))
}

pub fn archive_files(game_dir: &Path) -> Vec<PathBuf> {
    let mut files: Vec<PathBuf> = fs::read_dir(game_dir)
        .map(|it| it.flatten().map(|e| e.path()).filter(|p| is_archive(p)).collect())
        .unwrap_or_default();
    files.sort();
    files
}

/// Archive names, lengths and modification times; the index is rebuilt when they change.
pub fn fingerprint(game_dir: &Path) -> Vec<(String, u64, u128)> {
    archive_files(game_dir)
        .into_iter()
        .map(|p| {
            let meta = fs::metadata(&p).ok();
            let len = meta.as_ref().map_or(0, |m| m.len());
            let modified = meta
                .and_then(|m| m.modified().ok())
                .and_then(|t| t.duration_since(UNIX_EPOCH).ok())
                .map_or(0, |d| d.as_nanos());
            (p.file_name().unwrap_or_default().to_string_lossy().into_owned(), len, modified)
        })
        .collect()
}

fn walk_loose(base: &Path, dir: &Path, top: bool, out: &mut Vec<(String, u64)>) {
    let Ok(entries) = fs::read_dir(dir) else { return };
    for entry in entries.flatten() {
        let Ok(kind) = entry.file_type() else { continue };
        let name = entry.file_name().to_string_lossy().into_owned();
        if kind.is_dir() {
            if !(top && skip_dir(&name)) && !name.starts_with(".reomp") {
                walk_loose(base, &entry.path(), false, out);
            }
        } else if kind.is_file() && !top {
            let rel = entry.path().strip_prefix(base).unwrap_or(&entry.path()).to_path_buf();
            let path: Vec<String> = rel.components().map(|c| c.as_os_str().to_string_lossy().to_lowercase()).collect();
            out.push((path.join("/"), entry.metadata().map_or(0, |m| m.len())));
        }
    }
}

impl AssetIndex {
    pub fn is_current(&self, game_dir: &Path) -> bool {
        crate::targets::same_dir(&self.game_dir, game_dir) && self.fingerprint == fingerprint(game_dir)
    }

    pub fn build(game_dir: &Path) -> AssetIndex {
        let fingerprint = fingerprint(game_dir);
        let mut archives = Vec::new();
        let mut archive_errors = Vec::new();
        for file in archive_files(game_dir) {
            match Archive::open(&file) {
                Ok(archive) => archives.push(archive),
                Err(e) => archive_errors.push(format!("{}: {e}", file.file_name().unwrap_or_default().to_string_lossy())),
            }
        }
        let mut items = Vec::new();
        for (a, archive) in archives.iter().enumerate() {
            for (e, entry) in archive.entries.iter().enumerate() {
                items.push(Item { path: entry.path.clone(), source: a as u32, entry: e as u32, size: entry.size });
            }
        }
        let mut loose = Vec::new();
        walk_loose(game_dir, game_dir, true, &mut loose);
        items.extend(loose.into_iter().map(|(path, size)| Item { path, source: LOOSE, entry: 0, size }));
        items.sort_by(|a, b| a.path.cmp(&b.path).then(a.source.cmp(&b.source)));
        AssetIndex { game_dir: game_dir.to_path_buf(), archives, archive_errors, items, fingerprint }
    }

    pub fn source_name(&self, source: u32) -> String {
        if source == LOOSE {
            "loose file".into()
        } else {
            self.archives[source as usize].file.file_name().unwrap_or_default().to_string_lossy().into_owned()
        }
    }

    /// Items whose path starts with `prefix`, found by binary search.
    fn with_prefix(&self, prefix: &str) -> &[Item] {
        let start = self.items.partition_point(|i| i.path.as_str() < prefix);
        let end = start + self.items[start..].partition_point(|i| i.path.starts_with(prefix));
        &self.items[start..end]
    }

    /// The sub-folders and files directly inside `folder` ("" is the top).
    pub fn list(&self, folder: &str, limit: usize) -> (Vec<String>, Vec<&Item>, bool) {
        let prefix = if folder.is_empty() { String::new() } else { format!("{}/", folder.trim_end_matches('/')) };
        let mut folders = BTreeSet::new();
        let mut files = Vec::new();
        let mut more = false;
        for item in self.with_prefix(&prefix) {
            let rest = &item.path[prefix.len()..];
            match rest.split_once('/') {
                Some((sub, _)) => {
                    folders.insert(sub.to_string());
                }
                None if files.len() < limit => files.push(item),
                None => more = true,
            }
        }
        (folders.into_iter().collect(), files, more)
    }

    /// Items whose path contains every word of `query`, case-insensitive.
    pub fn search(&self, query: &str, folder: &str, limit: usize) -> (Vec<&Item>, bool) {
        let words: Vec<String> = query.split_whitespace().map(|w| w.trim_start_matches('*').to_lowercase()).collect();
        let prefix = if folder.is_empty() { String::new() } else { format!("{}/", folder.trim_end_matches('/')) };
        let mut found = Vec::new();
        for item in self.with_prefix(&prefix) {
            if words.iter().all(|w| item.path.contains(w.as_str())) {
                if found.len() == limit {
                    return (found, true);
                }
                found.push(item);
            }
        }
        (found, false)
    }

    pub fn find(&self, path: &str, source: u32) -> Option<&Item> {
        self.with_prefix(path).iter().find(|i| i.path == path && i.source == source)
    }

    /// Reads one item's bytes from its archive or the game folder.
    pub fn read(&self, item: &Item, codecs: &dyn Codecs) -> Result<Vec<u8>, String> {
        if item.source == LOOSE {
            let from = safe_join(&self.game_dir, &item.path)?;
            fs::read(&from).map_err(|e| format!("can't read {}: {e}", from.display()))
        } else {
            let archive = &self.archives[item.source as usize];
            let entry = &archive.entries[item.entry as usize];
            let mut file = File::open(&archive.file).map_err(|e| format!("can't open {}: {e}", archive.file.display()))?;
            dat::read_entry(&mut file, entry, codecs)
        }
    }

    /// Extracts one item to `<dest>/<path>`, writing a temporary file first.
    pub fn extract(&self, item: &Item, dest: &Path, codecs: &dyn Codecs) -> Result<PathBuf, String> {
        let target = safe_join(dest, &item.path)?;
        if let Some(parent) = target.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("can't create {}: {e}", parent.display()))?;
        }
        let bytes = if item.source == LOOSE {
            let from = safe_join(&self.game_dir, &item.path)?;
            fs::read(&from).map_err(|e| format!("can't read {}: {e}", from.display()))?
        } else {
            let archive = &self.archives[item.source as usize];
            let entry = &archive.entries[item.entry as usize];
            let mut file = File::open(&archive.file).map_err(|e| format!("can't open {}: {e}", archive.file.display()))?;
            dat::read_entry(&mut file, entry, codecs)?
        };
        let temp = target.with_extension("reomp-part");
        let mut out = File::create(&temp).map_err(|e| format!("can't write {}: {e}", temp.display()))?;
        out.write_all(&bytes).and_then(|_| out.sync_all()).map_err(|e| {
            let _ = fs::remove_file(&temp);
            format!("can't write {}: {e}", temp.display())
        })?;
        drop(out);
        fs::rename(&temp, &target).map_err(|e| {
            let _ = fs::remove_file(&temp);
            format!("can't write {}: {e}", target.display())
        })?;
        Ok(target)
    }
}

/// Joins a `/` path under `base`, refusing anything that could leave it.
pub fn safe_join(base: &Path, rel: &str) -> Result<PathBuf, String> {
    let mut path = base.to_path_buf();
    for part in rel.split('/') {
        let bad = part.is_empty() || part == "." || part == ".." || part.contains([':', '\\']) || part.ends_with(['.', ' ']);
        if bad {
            return Err(format!("\"{rel}\" isn't a safe file path"));
        }
        path.push(part);
    }
    Ok(path)
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ItemView {
    pub path: String,
    pub source: u32,
    pub source_name: String,
    pub size: u64,
}

impl ItemView {
    pub fn of(index: &AssetIndex, item: &Item) -> ItemView {
        ItemView { path: item.path.clone(), source: item.source, source_name: index.source_name(item.source), size: item.size }
    }
}

/// Oodle comes from the game's own `oo2core_*_win64.dll`, loaded on demand.
pub struct GameCodecs {
    #[cfg(windows)]
    oodle: Result<libloading::Library, String>,
}

impl GameCodecs {
    pub fn for_game(game_dir: &Path) -> GameCodecs {
        #[cfg(windows)]
        {
            let dll = fs::read_dir(game_dir).ok().and_then(|it| {
                it.flatten().map(|e| e.path()).find(|p| {
                    let n = p.file_name().unwrap_or_default().to_string_lossy().to_ascii_lowercase();
                    n.starts_with("oo2core_") && n.ends_with("_win64.dll")
                })
            });
            let oodle = match dll {
                // SAFETY: loads the game's own Oodle library, which has no load-time side effects we rely on.
                Some(path) => unsafe { libloading::Library::new(&path) }
                    .map_err(|e| format!("can't load the game's Oodle library {}: {e}", path.display())),
                None => Err("the game's Oodle library (oo2core_*_win64.dll) isn't in the game folder".into()),
            };
            GameCodecs { oodle }
        }
        #[cfg(not(windows))]
        {
            let _ = game_dir;
            GameCodecs {}
        }
    }
}

impl Codecs for GameCodecs {
    #[cfg(windows)]
    fn oodle(&self, input: &[u8], size: usize) -> Result<Vec<u8>, String> {
        type Decompress = unsafe extern "C" fn(
            *const u8, isize, *mut u8, isize, i32, i32, i32,
            *mut u8, isize, *mut u8, *mut u8, *mut u8, isize, i32,
        ) -> isize;
        let library = self.oodle.as_ref().map_err(|e| e.clone())?;
        // SAFETY: OodleLZ_Decompress has this signature in oo2core 8 and 9.
        let decompress: libloading::Symbol<Decompress> = unsafe { library.get(b"OodleLZ_Decompress\0") }
            .map_err(|e| format!("the game's Oodle library has no decompressor: {e}"))?;
        let mut out = vec![0u8; size];
        let null = std::ptr::null_mut();
        // SAFETY: both buffers are valid for the lengths passed; fuzz-safe decoding is on.
        let written = unsafe {
            decompress(input.as_ptr(), input.len() as isize, out.as_mut_ptr(), size as isize, 1, 0, 0, null, 0, null, null, null, 0, 3)
        };
        if written != size as isize {
            return Err(format!("an OODL chunk unpacked to {written} of {size} bytes"));
        }
        Ok(out)
    }

    #[cfg(not(windows))]
    fn oodle(&self, _input: &[u8], _size: usize) -> Result<Vec<u8>, String> {
        Err("it uses Oodle compression, which can only be extracted on Windows for now".into())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::temp_dir;

    #[test]
    fn indexes_loose_files_and_skips_manager_folders() {
        let game = temp_dir("assets");
        for rel in ["CHARS/MINIFIG/A.GHG", "CHARS/MINIFIG/B.TEXTURE", "mods/x/mod.json", "legacy-mods/m/y", "omp-data/z"] {
            let path = safe_join(&game, rel).unwrap();
            fs::create_dir_all(path.parent().unwrap()).unwrap();
            fs::write(path, b"data").unwrap();
        }
        fs::write(game.join("LEGOSTARWARSSKYWALKERSAGA_DX11.exe"), b"exe").unwrap();
        let index = AssetIndex::build(&game);
        let paths: Vec<&str> = index.items.iter().map(|i| i.path.as_str()).collect();
        assert_eq!(paths, vec!["chars/minifig/a.ghg", "chars/minifig/b.texture"]);
        assert!(index.is_current(&game));

        let (folders, files, _) = index.list("", 10);
        assert_eq!(folders, vec!["chars"]);
        assert!(files.is_empty());
        let (_, files, _) = index.list("chars/minifig", 10);
        assert_eq!(files.len(), 2);
        assert_eq!(index.search("*.ghg", "", 10).0.len(), 1);
        assert_eq!(index.search("minifig texture", "", 10).0.len(), 1);

        let out = temp_dir("assets-out");
        let item = index.find("chars/minifig/a.ghg", LOOSE).unwrap().clone();
        let written = index.extract(&item, &out, &GameCodecs::for_game(&game)).unwrap();
        assert_eq!(fs::read(written).unwrap(), b"data");
    }

    #[test]
    fn refuses_paths_that_leave_the_folder() {
        let base = Path::new("base");
        for bad in ["../x", "a/../../x", "c:/x", "a//b", "a/./b", "a\\b"] {
            assert!(safe_join(base, bad).is_err(), "{bad}");
        }
        assert!(safe_join(base, "chars/a.ghg").is_ok());
    }

    #[test]
    fn decodes_tt_deflate_like_the_reference() {
        // A stored block: standard type 0, TT type 2.
        let stored = [0b0000_0101u8, 5, 0, !5u8, 0xff, b'h', b'e', b'l', b'l', b'o'];
        assert_eq!(dat::inflate_tt(&stored, 5).unwrap(), b"hello");
        // A fixed-Huffman block (type 1 in both): raw deflate of "hello hello hello" from Python's zlib.
        let fixed = [0xcbu8, 0x48, 0xcd, 0xc9, 0xc9, 0x57, 0xc8, 0x40, 0x90, 0x00];
        assert_eq!(dat::inflate_tt(&fixed, 17).unwrap(), b"hello hello hello");
    }
}
