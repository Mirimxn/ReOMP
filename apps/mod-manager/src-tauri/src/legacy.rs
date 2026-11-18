//! Old OMP mod folders moved to `<game>/legacy-mods/` on first install, and
//! packing one into a versioned mod ZIP so it can join the library.

use std::collections::BTreeMap;
use std::fs::{self, File};
use std::io::{self, Write};
use std::path::{Path, PathBuf};

use serde_json::{Map, Value};
use zip::write::SimpleFileOptions;

use crate::deploy::LEGACY_DIR;

/// Version given to an old mod that never declared one.
pub const LEGACY_VERSION: &str = "0.1.0";
/// Minimum given to an old dependency that never declared one: any version.
pub const ANY_VERSION: &str = "0.0.0";

#[derive(Debug, Clone, PartialEq)]
pub struct LegacyMod {
    pub id: String,
    pub name: String,
    pub folder: PathBuf,
}

/// Mods found in each game's `legacy-mods/<backup>/<id>/mod.json`. When the
/// same ID is in several backups, the newest backup wins.
pub fn find(game_dirs: &[PathBuf]) -> Vec<LegacyMod> {
    let mut found: BTreeMap<String, (String, LegacyMod)> = BTreeMap::new();
    for game in game_dirs {
        let Ok(backups) = fs::read_dir(game.join(LEGACY_DIR)) else { continue };
        for backup in backups.flatten().filter(|e| e.path().is_dir()) {
            let backup_name = backup.file_name().to_string_lossy().into_owned();
            let Ok(mods) = fs::read_dir(backup.path()) else { continue };
            for entry in mods.flatten() {
                let folder = entry.path();
                let Ok(text) = fs::read_to_string(folder.join("mod.json")) else { continue };
                let id = entry.file_name().to_string_lossy().into_owned();
                let name = serde_json::from_str::<Value>(&text)
                    .ok()
                    .and_then(|v| v.get("name").and_then(|n| n.as_str()).map(str::to_string))
                    .unwrap_or_else(|| id.clone());
                let newer = found.get(&id).is_none_or(|(seen, _)| backup_name > *seen);
                if newer {
                    found.insert(id.clone(), (backup_name.clone(), LegacyMod { id, name, folder }));
                }
            }
        }
    }
    found.into_values().map(|(_, m)| m).collect()
}

/// Adds the fields the manager requires and keeps everything else as it was.
pub fn upgrade_manifest(text: &str) -> Result<String, String> {
    let mut value: Value = serde_json::from_str(text).map_err(|e| format!("mod.json isn't valid JSON: {e}"))?;
    let object = value.as_object_mut().ok_or("mod.json must be a JSON object")?;
    if !matches!(object.get("version"), Some(Value::String(_))) {
        object.insert("version".into(), Value::String(LEGACY_VERSION.into()));
    }
    if !matches!(object.get("actions"), Some(Value::Array(_))) {
        object.insert("actions".into(), Value::Array(Vec::new()));
    }
    let dependencies = match object.remove("dependencies") {
        Some(Value::Array(items)) => items,
        _ => Vec::new(),
    };
    let mut upgraded = Vec::new();
    for item in dependencies {
        let mut dependency = match item {
            Value::Object(map) => map,
            Value::String(id) => Map::from_iter([("id".to_string(), Value::String(id))]),
            other => return Err(format!("a dependency in mod.json isn't valid: {other}")),
        };
        if !matches!(dependency.get("minVersion"), Some(Value::String(_))) {
            dependency.insert("minVersion".into(), Value::String(ANY_VERSION.into()));
        }
        upgraded.push(Value::Object(dependency));
    }
    object.insert("dependencies".into(), Value::Array(upgraded));
    serde_json::to_string_pretty(&value).map_err(|e| e.to_string())
}

/// Packs `folder` as `<id>/...` into `out`, with an upgraded mod.json.
pub fn pack(folder: &Path, id: &str, out: &Path) -> Result<(), String> {
    let manifest = fs::read_to_string(folder.join("mod.json"))
        .map_err(|e| format!("Can't read {}: {e}", folder.join("mod.json").display()))?;
    let manifest = upgrade_manifest(&manifest)?;
    let file = File::create(out).map_err(|e| format!("Can't create {}: {e}", out.display()))?;
    let mut writer = zip::ZipWriter::new(file);
    let options = SimpleFileOptions::default();
    let mut files = Vec::new();
    collect(folder, folder, &mut files).map_err(|e| format!("Can't read {}: {e}", folder.display()))?;
    files.sort();
    let fail = |e: &dyn std::fmt::Display| format!("Can't write {}: {e}", out.display());
    writer.start_file(format!("{id}/mod.json"), options).map_err(|e| fail(&e))?;
    writer.write_all(manifest.as_bytes()).map_err(|e| fail(&e))?;
    for rel in files {
        if rel == "mod.json" {
            continue;
        }
        writer.start_file(format!("{id}/{rel}"), options).map_err(|e| fail(&e))?;
        let mut source = File::open(folder.join(&rel)).map_err(|e| format!("Can't read {rel}: {e}"))?;
        io::copy(&mut source, &mut writer).map_err(|e| fail(&e))?;
    }
    writer.finish().map_err(|e| fail(&e))?;
    Ok(())
}

/// Regular files under `dir`, relative to `base` with `/`. Links are skipped.
fn collect(base: &Path, dir: &Path, out: &mut Vec<String>) -> io::Result<()> {
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        let kind = entry.file_type()?;
        if kind.is_dir() {
            collect(base, &entry.path(), out)?;
        } else if kind.is_file() {
            let rel = entry.path().strip_prefix(base).unwrap_or(&entry.path()).to_path_buf();
            let parts: Vec<String> = rel.components().map(|c| c.as_os_str().to_string_lossy().into_owned()).collect();
            out.push(parts.join("/"));
        }
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::package::read_package;
    use crate::testutil::temp_dir;
    use crate::version::Version;

    #[test]
    fn upgrades_an_old_manifest() {
        let old = r#"{"name":"Gunship","actions":[{"typeId":"x"}],"dependencies":[{"id":"v1"},"c-api1"],"extra":1}"#;
        let upgraded: Value = serde_json::from_str(&upgrade_manifest(old).unwrap()).unwrap();
        assert_eq!(upgraded["version"], LEGACY_VERSION);
        assert_eq!(upgraded["dependencies"][0]["minVersion"], ANY_VERSION);
        assert_eq!(upgraded["dependencies"][1]["id"], "c-api1");
        assert_eq!(upgraded["actions"][0]["typeId"], "x");
        assert_eq!(upgraded["extra"], 1);
        let kept = upgrade_manifest(r#"{"name":"M","version":"2.0.0","actions":[],"dependencies":[{"id":"v1","minVersion":"1.0.0"}]}"#).unwrap();
        assert!(kept.contains("2.0.0") && kept.contains("1.0.0"));
    }

    #[test]
    fn finds_and_packs_the_inherited_gunship_fixture() {
        let game = temp_dir("legacy-game");
        let backup = game.join(LEGACY_DIR).join("mods-before-reomp-1");
        let folder = backup.join("republic-gunship");
        fs::create_dir_all(folder.join("icons")).unwrap();
        let fixture = include_str!("../../../../fixtures/vehicles/republic-gunship/mod.json");
        fs::write(folder.join("mod.json"), fixture).unwrap();
        fs::write(folder.join("icons/icon.png"), b"png").unwrap();
        fs::create_dir_all(backup.join("loose-folder")).unwrap();

        let found = find(&[game.clone()]);
        assert_eq!(found.len(), 1);
        assert_eq!(found[0].id, "republic-gunship");

        let zip = game.join("out.zip");
        pack(&found[0].folder, &found[0].id, &zip).unwrap();
        let info = read_package(&zip).unwrap();
        assert_eq!(info.id, "republic-gunship");
        assert_eq!(info.manifest.version, Version::parse(LEGACY_VERSION).unwrap());
        assert!(info.manifest.dependencies.iter().all(|d| d.min_version == Version(0, 0, 0)));
    }
}
