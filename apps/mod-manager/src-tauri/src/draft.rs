//! Builds an asset replacement mod from files the player picks for game
//! paths chosen in the Game files view.
//!
//! Until the asset-replacement provider exists (roadmap ASSET), the mod uses
//! v1's `register-custom-resource-action`, which links the mod's file into the
//! game folder and routes the game's lookup to it. v1 deletes a loose file at
//! the destination first, so only paths that exist solely inside the archives
//! can be replaced. See plans/mods/asset-replacement.md.

use std::collections::BTreeSet;
use std::fs::{self, File};
use std::io::{self, Write};
use std::path::{Path, PathBuf};

use serde::Deserialize;
use serde_json::json;
use zip::write::SimpleFileOptions;

use crate::assets::{AssetIndex, LOOSE};
use crate::version::Version;

pub const RESOURCE_ACTION: &str = "v1/register-custom-resource-action";
pub const PROVIDER: &str = "v1";
pub const PROVIDER_MIN: &str = "0.1.0";

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DraftFile {
    /// Lower-case game path from the index.
    pub game_path: String,
    /// The player's replacement file.
    pub file: PathBuf,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Draft {
    pub name: String,
    pub version: String,
    #[serde(default)]
    pub description: String,
    pub files: Vec<DraftFile>,
}

/// A portable mod ID from the name: lower-case letters, digits and dashes.
pub fn mod_id(name: &str) -> String {
    let mut id = String::new();
    for c in name.trim().chars() {
        if c.is_ascii_alphanumeric() {
            id.push(c.to_ascii_lowercase());
        } else if !id.ends_with('-') && !id.is_empty() {
            id.push('-');
        }
    }
    id.trim_end_matches('-').to_string()
}

/// Problems that stop the draft from becoming a mod, in plain language.
pub fn check(draft: &Draft, index: &AssetIndex) -> Vec<String> {
    let mut problems = Vec::new();
    if mod_id(&draft.name).is_empty() {
        problems.push("Give the mod a name with at least one letter or digit.".into());
    }
    if let Err(e) = Version::parse(draft.version.trim()) {
        problems.push(format!("Version: {e}."));
    }
    if draft.files.is_empty() {
        problems.push("Add at least one replacement file.".into());
    }
    let mut seen = BTreeSet::new();
    for file in &draft.files {
        let path = &file.game_path;
        if !seen.insert(path.to_lowercase()) {
            problems.push(format!("{path} is replaced twice. Keep one."));
        }
        let sources: Vec<u32> = index.items.iter().filter(|i| &i.path == path).map(|i| i.source).collect();
        if sources.is_empty() {
            problems.push(format!("{path} isn't one of the game's files."));
        } else if sources.contains(&LOOSE) {
            problems.push(format!(
                "{path} is a loose file in the game folder. Replacing loose files needs the upcoming asset provider, so pick a file that's only inside the archives."
            ));
        }
        if !file.file.is_file() {
            problems.push(format!("The replacement for {path} ({}) can't be found.", file.file.display()));
        }
    }
    problems
}

/// Writes the mod ZIP to `out`. Call `check` first.
pub fn write_zip(draft: &Draft, out: &Path) -> Result<String, String> {
    let id = mod_id(&draft.name);
    let mut actions = Vec::new();
    for file in &draft.files {
        let game_path = file.game_path.to_uppercase();
        actions.push(json!({
            "typeId": RESOURCE_ACTION,
            "payload": { "path": game_path, "srcPath": format!("files/{game_path}") },
        }));
    }
    let mut manifest = json!({
        "name": draft.name.trim(),
        "version": draft.version.trim(),
        "actions": actions,
        "dependencies": [{ "id": PROVIDER, "minVersion": PROVIDER_MIN }],
    });
    if !draft.description.trim().is_empty() {
        manifest["description"] = json!(draft.description.trim());
    }
    let fail = |e: &dyn std::fmt::Display| format!("Can't write {}: {e}", out.display());
    let mut writer = zip::ZipWriter::new(File::create(out).map_err(|e| fail(&e))?);
    let options = SimpleFileOptions::default();
    writer.start_file(format!("{id}/mod.json"), options).map_err(|e| fail(&e))?;
    let text = serde_json::to_string_pretty(&manifest).map_err(|e| fail(&e))?;
    writer.write_all(text.as_bytes()).map_err(|e| fail(&e))?;
    for file in &draft.files {
        writer
            .start_file(format!("{id}/files/{}", file.game_path.to_uppercase()), options)
            .map_err(|e| fail(&e))?;
        let mut source = File::open(&file.file).map_err(|e| format!("Can't read {}: {e}", file.file.display()))?;
        io::copy(&mut source, &mut writer).map_err(|e| fail(&e))?;
    }
    writer.finish().map_err(|e| fail(&e))?;
    Ok(id)
}

/// Writes to a temporary file beside `out`, checks it as a mod package, then renames.
pub fn export(draft: &Draft, out: &Path) -> Result<String, String> {
    let temp = out.with_extension("zip.part");
    let result = write_zip(draft, &temp).and_then(|id| {
        crate::package::read_package(&temp).map_err(|e| format!("The generated mod isn't valid: {e}"))?;
        fs::rename(&temp, out).map_err(|e| format!("Can't save {}: {e}", out.display()))?;
        Ok(id)
    });
    if result.is_err() {
        let _ = fs::remove_file(&temp);
    }
    result
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::assets::Item;
    use crate::testutil::temp_dir;

    fn index_with(items: &[(&str, u32)]) -> AssetIndex {
        let mut index = AssetIndex::build(&temp_dir("draft-game"));
        index.items = items.iter().map(|(p, s)| Item { path: p.to_string(), source: *s, entry: 0, size: 1 }).collect();
        index
    }

    #[test]
    fn makes_a_portable_id() {
        assert_eq!(mod_id("  Clone Trooper: Red stripes! "), "clone-trooper-red-stripes");
        assert_eq!(mod_id("¡!"), "");
    }

    #[test]
    fn builds_a_two_file_replacement_mod() {
        let dir = temp_dir("draft");
        let (a, b) = (dir.join("a.ghg"), dir.join("b.texture"));
        fs::write(&a, b"model").unwrap();
        fs::write(&b, b"texture").unwrap();
        let draft = Draft {
            name: "Red Troopers".into(),
            version: "1.0.0".into(),
            description: "Red stripes".into(),
            files: vec![
                DraftFile { game_path: "chars/minifig/clonetrooper/clonetrooper.ghg".into(), file: a },
                DraftFile { game_path: "chars/minifig/clonetrooper/stripes.texture".into(), file: b },
            ],
        };
        let index = index_with(&[("chars/minifig/clonetrooper/clonetrooper.ghg", 0), ("chars/minifig/clonetrooper/stripes.texture", 3)]);
        assert!(check(&draft, &index).is_empty(), "{:?}", check(&draft, &index));

        let out = dir.join("red-troopers.zip");
        assert_eq!(export(&draft, &out).unwrap(), "red-troopers");
        let info = crate::package::read_package(&out).unwrap();
        assert_eq!(info.id, "red-troopers");
        assert_eq!(info.manifest.dependencies[0].id, PROVIDER);
        let mut archive = zip::ZipArchive::new(File::open(&out).unwrap()).unwrap();
        let mut manifest = String::new();
        io::Read::read_to_string(&mut archive.by_name("red-troopers/mod.json").unwrap(), &mut manifest).unwrap();
        let manifest: serde_json::Value = serde_json::from_str(&manifest).unwrap();
        assert_eq!(manifest["actions"][0]["payload"]["path"], "CHARS/MINIFIG/CLONETROOPER/CLONETROOPER.GHG");
        assert_eq!(manifest["actions"][1]["payload"]["srcPath"], "files/CHARS/MINIFIG/CLONETROOPER/STRIPES.TEXTURE");
        assert!(archive.by_name("red-troopers/files/CHARS/MINIFIG/CLONETROOPER/STRIPES.TEXTURE").is_ok());
    }

    #[test]
    fn rejects_loose_unknown_and_duplicate_destinations() {
        let dir = temp_dir("draft-bad");
        let a = dir.join("a");
        fs::write(&a, b"x").unwrap();
        let file = |p: &str| DraftFile { game_path: p.into(), file: a.clone() };
        let index = index_with(&[("chars/a.ghg", 0), ("chars/loose.ghg", 0), ("chars/loose.ghg", LOOSE)]);
        let draft = Draft {
            name: "!!".into(),
            version: "1.0".into(),
            description: String::new(),
            files: vec![file("chars/a.ghg"), file("chars/a.ghg"), file("chars/loose.ghg"), file("chars/missing.ghg")],
        };
        let problems = check(&draft, &index);
        assert_eq!(problems.len(), 5, "{problems:?}");
    }
}
