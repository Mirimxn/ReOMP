//! A new version of a mod in the library, made from an installed version with
//! some files replaced or added (the character maker's "update its own mod").
//! See plans/mod-manager.md#character-maker.

use std::collections::BTreeMap;
use std::fs::File;
use std::io::{Cursor, Read, Write};
use std::path::Path;

use zip::write::SimpleFileOptions;

use crate::version::Version;

/// The mod folder inside a package ZIP (the folder holding mod.json) and its raw mod.json.
pub fn read_manifest(zip_path: &Path) -> Result<(String, String), String> {
    let file = File::open(zip_path).map_err(|e| format!("Can't open {}: {e}", zip_path.display()))?;
    let mut archive = zip::ZipArchive::new(file).map_err(|e| format!("Can't read {}: {e}", zip_path.display()))?;
    let name = archive
        .file_names()
        .find(|n| n.split('/').count() == 2 && n.ends_with("/mod.json"))
        .map(str::to_string)
        .ok_or_else(|| format!("{} has no <mod>/mod.json", zip_path.display()))?;
    let mut text = String::new();
    archive
        .by_name(&name)
        .map_err(|e| e.to_string())?
        .read_to_string(&mut text)
        .map_err(|e| format!("Can't read {name}: {e}"))?;
    Ok((name.trim_end_matches("/mod.json").to_string(), text))
}

/// The files of a ZIP whose entries are relative to the mod folder (as the
/// character maker sends them), by path.
pub fn read_overrides(bytes: &[u8]) -> Result<Vec<(String, Vec<u8>)>, String> {
    let mut archive = zip::ZipArchive::new(Cursor::new(bytes)).map_err(|e| format!("The changed files aren't a ZIP: {e}"))?;
    let mut files = Vec::new();
    for i in 0..archive.len() {
        let mut entry = archive.by_index(i).map_err(|e| e.to_string())?;
        if entry.is_dir() {
            continue;
        }
        let name = entry.name().replace('\\', "/");
        if name.is_empty() || name.starts_with('/') || name.split('/').any(|s| s.is_empty() || s == "." || s == "..") {
            return Err(format!("\"{name}\" isn't a plain path inside the mod"));
        }
        let mut data = Vec::new();
        entry.read_to_end(&mut data).map_err(|e| format!("Can't read {name}: {e}"))?;
        files.push((name, data));
    }
    Ok(files)
}

/// Writes `out`: every file of `old` with `changes` replacing files of the same
/// path (letter case ignored) and adding the rest, and mod.json (from `changes`
/// if it has one) set to `version`.
pub fn write_next_version(old: &Path, changes: &[(String, Vec<u8>)], version: Version, out: &Path) -> Result<(), String> {
    let (folder, old_manifest) = read_manifest(old)?;
    let key = |p: &str| p.to_ascii_lowercase();
    let changed: BTreeMap<String, &(String, Vec<u8>)> = changes.iter().map(|c| (key(&c.0), c)).collect();
    let manifest_text = changed.get("mod.json").map_or(old_manifest, |c| String::from_utf8_lossy(&c.1).into_owned());
    let mut manifest: serde_json::Value = serde_json::from_str(&manifest_text).map_err(|e| format!("mod.json isn't valid JSON: {e}"))?;
    manifest
        .as_object_mut()
        .ok_or("mod.json isn't a JSON object")?
        .insert("version".into(), serde_json::Value::String(version.to_string()));

    let source = File::open(old).map_err(|e| format!("Can't open {}: {e}", old.display()))?;
    let mut archive = zip::ZipArchive::new(source).map_err(|e| format!("Can't read {}: {e}", old.display()))?;
    let fail = |e: &dyn std::fmt::Display| format!("Can't write {}: {e}", out.display());
    let mut writer = zip::ZipWriter::new(File::create(out).map_err(|e| fail(&e))?);
    let options = SimpleFileOptions::default();
    writer.start_file(format!("{folder}/mod.json"), options).map_err(|e| fail(&e))?;
    let text = serde_json::to_string_pretty(&manifest).map_err(|e| e.to_string())?;
    writer.write_all(text.as_bytes()).map_err(|e| fail(&e))?;
    let prefix = format!("{folder}/");
    for i in 0..archive.len() {
        let mut entry = archive.by_index(i).map_err(|e| e.to_string())?;
        let Some(rel) = entry.name().strip_prefix(&prefix).map(str::to_string) else { continue };
        if entry.is_dir() || rel.is_empty() || key(&rel) == "mod.json" || changed.contains_key(&key(&rel)) {
            continue;
        }
        writer.start_file(format!("{prefix}{rel}"), options).map_err(|e| fail(&e))?;
        std::io::copy(&mut entry, &mut writer).map_err(|e| fail(&e))?;
    }
    for (rel, data) in changes {
        if key(rel) == "mod.json" {
            continue;
        }
        writer.start_file(format!("{prefix}{rel}"), options).map_err(|e| fail(&e))?;
        writer.write_all(data).map_err(|e| fail(&e))?;
    }
    writer.finish().map_err(|e| fail(&e))?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::{temp_dir, write_zip, Entry};

    #[test]
    fn next_version_replaces_and_adds_files_and_sets_the_version() {
        let dir = temp_dir("update-next-version");
        let old = dir.join("trooper-1.0.0.zip");
        write_zip(
            &old,
            &[
                Entry::File("trooper/mod.json", br#"{"name":"Trooper","version":"1.0.0","dependencies":[],"actions":[]}"#),
                Entry::File("trooper/CHARS/MINIFIG/T/T.PREFAB_BAKED", b"old prefab"),
                Entry::File("trooper/CHARS/MINIFIG/T/T_HEAD.TEXTURE", b"head"),
                Entry::File("trooper/icon.png", b"icon"),
            ],
        );
        let changes = vec![
            ("CHARS/minifig/t/t.prefab_baked".to_string(), b"new prefab".to_vec()),
            ("CHARS/MINIFIG/T/T_ARM.TEXTURE".to_string(), b"arm".to_vec()),
            (
                "mod.json".to_string(),
                br#"{"name":"Trooper","version":"1.0.0","dependencies":[],"actions":[{"typeId":"asset-replacement/register","payload":{"path":"a","srcPath":"b"}}]}"#.to_vec(),
            ),
        ];
        let out = dir.join("trooper-1.0.1.zip");
        write_next_version(&old, &changes, Version(1, 0, 1), &out).unwrap();

        let info = crate::package::read_package(&out).unwrap();
        assert_eq!(info.id, "trooper");
        assert_eq!(info.manifest.version, Version(1, 0, 1));
        let mut archive = zip::ZipArchive::new(File::open(&out).unwrap()).unwrap();
        let mut names: Vec<String> = archive.file_names().map(str::to_string).collect();
        names.sort();
        assert_eq!(
            names,
            [
                "trooper/CHARS/MINIFIG/T/T_ARM.TEXTURE",
                "trooper/CHARS/MINIFIG/T/T_HEAD.TEXTURE",
                "trooper/CHARS/minifig/t/t.prefab_baked",
                "trooper/icon.png",
                "trooper/mod.json",
            ]
        );
        let mut prefab = String::new();
        archive.by_name("trooper/CHARS/minifig/t/t.prefab_baked").unwrap().read_to_string(&mut prefab).unwrap();
        assert_eq!(prefab, "new prefab");
        let (_, manifest) = read_manifest(&out).unwrap();
        assert!(manifest.contains("asset-replacement/register"), "{manifest}");
    }

    #[test]
    fn overrides_must_stay_inside_the_mod() {
        let dir = temp_dir("update-overrides");
        let zip = dir.join("x.zip");
        write_zip(&zip, &[Entry::File("../escape.txt", b"x")]);
        let bytes = std::fs::read(&zip).unwrap();
        assert!(read_overrides(&bytes).unwrap_err().contains("plain path"));
    }
}
