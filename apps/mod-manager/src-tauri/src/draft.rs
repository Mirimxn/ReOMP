//! Builds an asset replacement mod from files the player picks for game
//! paths chosen in the Game files view.
//!
//! The mod declares each file with v1's `register-custom-resource-action`.
//! The manager places the file at its game path during deployment and sets
//! aside any loose original until the mod no longer needs it; v1 routes the
//! game's lookup at runtime until the asset-replacement provider takes over
//! (roadmap ASSET). See plans/mods/asset-replacement.md.

use std::collections::{BTreeMap, BTreeSet};
use std::fs::{self, File};
use std::io::{self, Write};
use std::path::{Path, PathBuf};

use serde::Deserialize;
use serde_json::json;
use zip::write::SimpleFileOptions;

use crate::assets::AssetIndex;
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
        if !index.items.iter().any(|i| &i.path == path) {
            problems.push(format!("{path} isn't one of the game's files."));
        } else if let Err(e) = crate::package::check_game_path(path) {
            problems.push(format!("{e}."));
        }
        if !file.file.is_file() {
            problems.push(format!("The replacement for {path} ({}) can't be found.", file.file.display()));
        }
    }
    problems
}

/// A `.texture` file's header length (including its leading size field) and
/// the resource name stored in it. The game drops a texture whose stored name
/// isn't the path it asked for.
pub fn texture_header(bytes: &[u8]) -> Option<(usize, String)> {
    if bytes.len() < 22 || &bytes[4..8] != b".CC4" {
        return None;
    }
    let header = 4 + u32::from_be_bytes([bytes[0], bytes[1], bytes[2], bytes[3]]) as usize;
    let name_len = u16::from_be_bytes([bytes[20], bytes[21]]) as usize;
    if header > bytes.len() || 22 + name_len > header {
        return None;
    }
    let name = bytes[22..22 + name_len].iter().take_while(|&&b| b != 0).map(|&b| b as char).collect();
    Some((header, name))
}

fn names_match(stored: &str, game_path: &str) -> bool {
    stored.replace('\\', "/").trim_start_matches('/').eq_ignore_ascii_case(game_path)
}

/// The new image with the header of the texture currently at that path, so
/// the stored name and ID are the ones the game expects.
pub fn rebuild_texture(new: &[u8], current: &[u8]) -> Result<Vec<u8>, String> {
    let (new_header, _) = texture_header(new).ok_or("it isn't a game texture file")?;
    let (current_header, _) = texture_header(current).ok_or("the game's texture at that path couldn't be read")?;
    if new.len() - new_header != current.len() - current_header {
        return Err("its image is a different size or format from the game's texture. Export it with the same size and format".into());
    }
    let mut out = current[..current_header].to_vec();
    out.extend_from_slice(&new[new_header..]);
    Ok(out)
}

/// Textures whose stored name doesn't match their destination, rebuilt with
/// the header of the texture the game currently has there. Returns the fixed
/// bytes by game path, notes for the player, and problems that block the build.
pub fn prepare_textures(
    draft: &Draft,
    index: &AssetIndex,
    codecs: &dyn crate::dat::Codecs,
) -> (BTreeMap<String, Vec<u8>>, Vec<String>, Vec<String>) {
    let (mut fixed, mut notes, mut problems) = (BTreeMap::new(), Vec::new(), Vec::new());
    for file in draft.files.iter().filter(|f| f.game_path.to_ascii_lowercase().ends_with(".texture")) {
        let Ok(new) = fs::read(&file.file) else { continue };
        let short = file.game_path.rsplit('/').next().unwrap_or(&file.game_path);
        let Some((_, stored)) = texture_header(&new) else {
            problems.push(format!("The replacement for {short} isn't a game texture file. Pick a .texture exported for this game."));
            continue;
        };
        if names_match(&stored, &file.game_path) {
            continue;
        }
        let current = index
            .items
            .iter()
            .filter(|i| i.path == file.game_path)
            .min_by_key(|i| i.source != crate::assets::LOOSE)
            .ok_or_else(|| "the game has no texture at that path".to_string())
            .and_then(|item| index.read(item, codecs));
        match current.and_then(|current| rebuild_texture(&new, &current)) {
            Ok(bytes) => {
                notes.push(format!("{short}: the file said it was {stored}; its name and ID were set to the game's texture."));
                fixed.insert(file.game_path.clone(), bytes);
            }
            Err(e) => problems.push(format!(
                "The replacement for {short} says it is {stored}, and it can't be fixed automatically: {e}."
            )),
        }
    }
    (fixed, notes, problems)
}

/// Writes the mod ZIP to `out`. Call `check` first. `fixed` replaces the
/// player's file for those game paths.
pub fn write_zip_with(draft: &Draft, fixed: &BTreeMap<String, Vec<u8>>, out: &Path) -> Result<String, String> {
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
        if let Some(bytes) = fixed.get(&file.game_path) {
            writer.write_all(bytes).map_err(|e| fail(&e))?;
        } else {
            let mut source = File::open(&file.file).map_err(|e| format!("Can't read {}: {e}", file.file.display()))?;
            io::copy(&mut source, &mut writer).map_err(|e| fail(&e))?;
        }
    }
    writer.finish().map_err(|e| fail(&e))?;
    Ok(id)
}

pub fn write_zip(draft: &Draft, out: &Path) -> Result<String, String> {
    write_zip_with(draft, &BTreeMap::new(), out)
}

/// Writes to a temporary file beside `out`, checks it as a mod package, then renames.
pub fn export(draft: &Draft, out: &Path) -> Result<String, String> {
    export_with(draft, &BTreeMap::new(), out)
}

pub fn export_with(draft: &Draft, fixed: &BTreeMap<String, Vec<u8>>, out: &Path) -> Result<String, String> {
    let temp = out.with_extension("zip.part");
    let result = write_zip_with(draft, fixed, &temp).and_then(|id| {
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

    fn texture(name: &str, id: u8, image: &[u8]) -> Vec<u8> {
        let mut header = b".CC4HSERHSER\0\0\0\x0e".to_vec();
        header.extend_from_slice(&((name.len() + 1) as u16).to_be_bytes());
        header.extend_from_slice(name.as_bytes());
        header.push(0);
        header.extend_from_slice(&[id; 16]);
        let mut out = (header.len() as u32).to_be_bytes().to_vec();
        out.extend_from_slice(&header);
        out.extend_from_slice(image);
        out
    }

    #[test]
    fn rebuilds_a_texture_made_from_another_one() {
        let dir = temp_dir("draft-texture");
        let game_path = "chars/super_character_texture/body/body_ct212kama_back_diff_dx11.texture";
        let current = texture("Chars/Super_Character_Texture/BODY/BODY_CT212KAMA_BACK_DIFF_DX11.texture", 1, b"old image");
        let new = texture("Chars/Super_Character_Texture/BODY/BODY_CL_AIRBORN_BACK_DIFF_DX11.texture", 2, b"new image");
        assert_eq!(texture_header(&new).unwrap().1, "Chars/Super_Character_Texture/BODY/BODY_CL_AIRBORN_BACK_DIFF_DX11.texture");

        let rebuilt = rebuild_texture(&new, &current).unwrap();
        assert_eq!(rebuilt, texture("Chars/Super_Character_Texture/BODY/BODY_CT212KAMA_BACK_DIFF_DX11.texture", 1, b"new image"));
        assert!(rebuild_texture(&texture("x", 2, b"bigger image"), &current).is_err());

        // Through the draft: the game's current texture is a loose file.
        let game = dir.join("game");
        let loose = crate::assets::safe_join(&game, game_path).unwrap();
        fs::create_dir_all(loose.parent().unwrap()).unwrap();
        fs::write(&loose, &current).unwrap();
        let picked = dir.join("picked.texture");
        fs::write(&picked, &new).unwrap();
        let mut index = AssetIndex::build(&game);
        index.items.retain(|i| i.path == game_path);
        let draft = Draft {
            name: "Update".into(),
            version: "1.0.0".into(),
            description: String::new(),
            files: vec![DraftFile { game_path: game_path.into(), file: picked }],
        };
        let codecs = crate::assets::GameCodecs::for_game(&game);
        let (fixed, notes, problems) = prepare_textures(&draft, &index, &codecs);
        assert!(problems.is_empty(), "{problems:?}");
        assert_eq!(notes.len(), 1);
        assert_eq!(fixed[game_path], rebuilt);

        let out = dir.join("update.zip");
        export_with(&draft, &fixed, &out).unwrap();
        let mut archive = zip::ZipArchive::new(File::open(&out).unwrap()).unwrap();
        let mut stored = Vec::new();
        io::Read::read_to_end(
            &mut archive.by_name(&format!("update/files/{}", game_path.to_uppercase())).unwrap(),
            &mut stored,
        )
        .unwrap();
        assert_eq!(stored, rebuilt);
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
    fn rejects_unknown_reserved_and_duplicate_destinations() {
        let dir = temp_dir("draft-bad");
        let a = dir.join("a");
        fs::write(&a, b"x").unwrap();
        let file = |p: &str| DraftFile { game_path: p.into(), file: a.clone() };
        let index = index_with(&[("chars/a.ghg", 0), ("chars/loose.ghg", crate::assets::LOOSE), ("game.dat", crate::assets::LOOSE)]);
        let draft = Draft {
            name: "!!".into(),
            version: "1.0".into(),
            description: String::new(),
            files: vec![file("chars/a.ghg"), file("chars/a.ghg"), file("chars/loose.ghg"), file("chars/missing.ghg"), file("game.dat")],
        };
        // Name, version, the duplicate, the unknown path and the reserved archive; the loose file is fine.
        let problems = check(&draft, &index);
        assert_eq!(problems.len(), 5, "{problems:?}");
    }
}
