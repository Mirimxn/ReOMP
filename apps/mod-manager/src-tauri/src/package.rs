//! Reads and validates one mod ZIP.
//! Layout: notes/omp/mod-packages.md. Rules: plans/mod-manager.md#packages-and-versions.

use std::collections::HashSet;
use std::fs::File;
use std::io::{self, Read};
use std::path::Path;

use serde::Serialize;
use serde_json::Value;
use sha2::{Digest, Sha256};

use crate::version::Version;

const MAX_MANIFEST_BYTES: u64 = 1024 * 1024;

#[derive(Debug, Clone, PartialEq, Eq, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Dependency {
    pub id: String,
    pub min_version: Version,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Manifest {
    pub name: String,
    pub description: Option<String>,
    pub version: Version,
    pub dependencies: Vec<Dependency>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct PackageInfo {
    /// Exact mod ID: the ZIP's single top-level folder.
    pub id: String,
    pub manifest: Manifest,
    /// SHA-256 of the whole ZIP file, lowercase hex.
    pub sha256: String,
}

pub fn sha256_file(path: &Path) -> io::Result<String> {
    let mut file = File::open(path)?;
    let mut hasher = Sha256::new();
    io::copy(&mut file, &mut hasher)?;
    Ok(hasher.finalize().iter().map(|b| format!("{b:02x}")).collect())
}

/// Validates the ZIP's paths and manifest. The error is a plain-language reason.
pub fn read_package(path: &Path) -> Result<PackageInfo, String> {
    let file = File::open(path).map_err(|e| format!("can't open the file: {e}"))?;
    let mut archive = zip::ZipArchive::new(file).map_err(|e| format!("not a readable ZIP: {e}"))?;

    let mut root: Option<String> = None;
    let mut files: HashSet<String> = HashSet::new();
    let mut dirs: HashSet<String> = HashSet::new();
    let mut seen: HashSet<String> = HashSet::new();
    let mut manifest_name: Option<String> = None;

    for index in 0..archive.len() {
        let entry = archive
            .by_index_raw(index)
            .map_err(|e| format!("can't read ZIP entry {index}: {e}"))?;
        let raw = entry.name().to_string();
        let is_dir = entry.is_dir();
        if let Some(mode) = entry.unix_mode() {
            if mode & 0o170000 == 0o120000 {
                return Err(format!("\"{raw}\" is a link; links aren't allowed in mod packages"));
            }
        }
        let segments = check_entry_path(&raw, is_dir)?;

        match &root {
            None => root = Some(segments[0].clone()),
            Some(r) if *r != segments[0] => {
                return Err(format!(
                    "the ZIP must contain exactly one top-level folder (found \"{r}\" and \"{}\")",
                    segments[0]
                ));
            }
            _ => {}
        }
        if segments.len() == 1 && !is_dir {
            return Err(format!(
                "\"{raw}\" is outside the mod folder; everything must be inside one top-level folder named with the mod ID"
            ));
        }

        let key = segments.iter().map(|s| fold(s)).collect::<Vec<_>>().join("/");
        if !seen.insert(format!("{}{}", key, if is_dir { "/" } else { "" })) {
            return Err(format!("\"{raw}\" appears more than once (ignoring letter case)"));
        }
        for depth in 1..segments.len() {
            dirs.insert(segments[..depth].iter().map(|s| fold(s)).collect::<Vec<_>>().join("/"));
        }
        if is_dir {
            dirs.insert(key);
        } else {
            if segments.len() == 2 && segments[1] == "mod.json" {
                manifest_name = Some(raw.clone());
            }
            files.insert(key);
        }
    }

    let Some(id) = root else {
        return Err("the ZIP is empty".into());
    };
    if let Some(clash) = files.iter().find(|f| dirs.contains(*f)) {
        return Err(format!("\"{clash}\" is both a file and a folder (ignoring letter case)"));
    }
    let Some(manifest_name) = manifest_name else {
        return Err(format!("\"{id}/mod.json\" is missing"));
    };

    let text = {
        let entry = archive
            .by_name(&manifest_name)
            .map_err(|e| format!("can't read mod.json: {e}"))?;
        if entry.size() > MAX_MANIFEST_BYTES {
            return Err("mod.json is larger than 1 MB".into());
        }
        let mut text = String::new();
        entry
            .take(MAX_MANIFEST_BYTES + 1)
            .read_to_string(&mut text)
            .map_err(|e| format!("can't read mod.json as UTF-8 text: {e}"))?;
        text
    };
    let manifest = parse_manifest(&text)?;
    drop(archive);

    let sha256 = sha256_file(path).map_err(|e| format!("can't read the file: {e}"))?;
    Ok(PackageInfo { id, manifest, sha256 })
}

fn fold(segment: &str) -> String {
    segment.to_lowercase()
}

/// Splits a ZIP entry name into safe path segments, rejecting anything that
/// could escape the mod folder or misbehave on Windows.
fn check_entry_path(raw: &str, is_dir: bool) -> Result<Vec<String>, String> {
    if raw.is_empty() {
        return Err("a ZIP entry has an empty name".into());
    }
    if raw.contains('\\') {
        return Err(format!("\"{raw}\" uses backslashes; ZIP paths must use /"));
    }
    if raw.starts_with('/') || raw.contains(':') {
        return Err(format!("\"{raw}\" is an absolute path or contains ':'"));
    }
    let trimmed = if is_dir { raw.strip_suffix('/').unwrap_or(raw) } else { raw };
    let mut segments = Vec::new();
    for segment in trimmed.split('/') {
        if segment.is_empty() || segment == "." || segment == ".." {
            return Err(format!("\"{raw}\" contains an empty, \".\" or \"..\" path part"));
        }
        if segment.ends_with('.') || segment.ends_with(' ') {
            return Err(format!("\"{raw}\": names can't end with a dot or space on Windows"));
        }
        if segment.chars().any(|c| c.is_control() || "<>\"|?*".contains(c)) {
            return Err(format!("\"{raw}\" contains a character Windows doesn't allow"));
        }
        let stem = segment.split('.').next().unwrap_or("").trim_end().to_ascii_uppercase();
        let reserved = matches!(stem.as_str(), "CON" | "PRN" | "AUX" | "NUL")
            || ((stem.starts_with("COM") || stem.starts_with("LPT"))
                && stem.len() == 4
                && matches!(stem.as_bytes()[3], b'0'..=b'9'));
        if reserved {
            return Err(format!("\"{raw}\" uses \"{segment}\", a name Windows reserves"));
        }
        segments.push(segment.to_string());
    }
    Ok(segments)
}

pub fn parse_manifest(text: &str) -> Result<Manifest, String> {
    let value: Value = serde_json::from_str(text).map_err(|e| format!("mod.json isn't valid JSON: {e}"))?;
    let object = value.as_object().ok_or("mod.json must be a JSON object")?;

    let name = match object.get("name") {
        Some(Value::String(s)) if !s.trim().is_empty() => s.clone(),
        _ => return Err("mod.json needs a \"name\"".into()),
    };
    let description = match object.get("description") {
        None | Some(Value::Null) => None,
        Some(Value::String(s)) => Some(s.clone()),
        Some(_) => return Err("\"description\" in mod.json must be text".into()),
    };
    let version = match object.get("version") {
        Some(Value::String(s)) => Version::parse(s).map_err(|e| format!("\"version\" in mod.json: {e}"))?,
        _ => return Err("mod.json needs a \"version\" such as \"1.0.0\"".into()),
    };
    if !matches!(object.get("actions"), Some(Value::Array(_))) {
        return Err("mod.json needs an \"actions\" list (use [] for none)".into());
    }
    let Some(Value::Array(entries)) = object.get("dependencies") else {
        return Err("mod.json needs a \"dependencies\" list (use [] for none)".into());
    };
    let mut dependencies = Vec::new();
    for entry in entries {
        let id = match entry.get("id") {
            Some(Value::String(s)) if !s.is_empty() => s.clone(),
            _ => return Err("every dependency in mod.json needs an \"id\"".into()),
        };
        let min_version = match entry.get("minVersion") {
            Some(Value::String(s)) => {
                Version::parse(s).map_err(|e| format!("\"minVersion\" of dependency {id}: {e}"))?
            }
            _ => return Err(format!("dependency \"{id}\" in mod.json needs a \"minVersion\" such as \"1.0.0\"")),
        };
        dependencies.push(Dependency { id, min_version });
    }
    Ok(Manifest { name, description, version, dependencies })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::{manifest, temp_dir, write_zip, Entry};

    fn read(entries: &[Entry]) -> Result<PackageInfo, String> {
        let dir = temp_dir("package");
        let path = dir.join("test.zip");
        write_zip(&path, entries);
        read_package(&path)
    }

    #[test]
    fn reads_a_valid_package() {
        let json = manifest("Gunship", "1.2.0", &[("v1", "1.0.0")]);
        let info = read(&[
            Entry::Dir("gunship/"),
            Entry::File("gunship/mod.json", json.as_bytes()),
            Entry::File("gunship/icon.png", b"png"),
        ])
        .unwrap();
        assert_eq!(info.id, "gunship");
        assert_eq!(info.manifest.version, Version(1, 2, 0));
        assert_eq!(info.manifest.dependencies[0].id, "v1");
        assert_eq!(info.sha256.len(), 64);
    }

    #[test]
    fn rejects_bad_layouts() {
        let json = manifest("M", "1.0.0", &[]);
        let j = json.as_bytes();
        let cases: Vec<(&str, Vec<Entry>)> = vec![
            ("extra root", vec![Entry::File("a/mod.json", j), Entry::File("b/x.txt", b"x")]),
            ("file at root", vec![Entry::File("a/mod.json", j), Entry::File("readme.txt", b"x")]),
            ("traversal", vec![Entry::File("a/mod.json", j), Entry::File("a/../x.txt", b"x")]),
            ("absolute", vec![Entry::File("/a/mod.json", j)]),
            ("drive", vec![Entry::File("a/mod.json", j), Entry::File("a/C:x", b"x")]),
            ("reserved", vec![Entry::File("a/mod.json", j), Entry::File("a/CON.txt", b"x")]),
            ("trailing dot", vec![Entry::File("a/mod.json", j), Entry::File("a/x.", b"x")]),
            ("case clash", vec![Entry::File("a/mod.json", j), Entry::File("a/Icon.png", b"1"), Entry::File("a/icon.PNG", b"2")]),
            ("file vs folder", vec![Entry::File("a/mod.json", j), Entry::File("a/tex", b"1"), Entry::File("a/TEX/x", b"2")]),
            ("link", vec![Entry::File("a/mod.json", j), Entry::Link("a/link", "../../etc")]),
            ("no manifest", vec![Entry::File("a/x.txt", b"x")]),
        ];
        for (label, entries) in cases {
            assert!(read(&entries).is_err(), "{label} should be rejected");
        }
    }

    #[test]
    fn rejects_bad_manifests() {
        let bad = [
            r#"{"name":"M","actions":[],"dependencies":[]}"#,
            r#"{"name":"M","version":"1.0","actions":[],"dependencies":[]}"#,
            r#"{"version":"1.0.0","actions":[],"dependencies":[]}"#,
            r#"{"name":"M","version":"1.0.0","dependencies":[]}"#,
            r#"{"name":"M","version":"1.0.0","actions":[]}"#,
            r#"{"name":"M","version":"1.0.0","actions":[],"dependencies":[{"id":"v1"}]}"#,
            r#"not json"#,
        ];
        for text in bad {
            assert!(parse_manifest(text).is_err(), "{text} should be rejected");
        }
        let ok = parse_manifest(r#"{"name":"M","description":"d","version":"0.1.0","actions":[],"dependencies":[]}"#).unwrap();
        assert_eq!(ok.description.as_deref(), Some("d"));
    }

    #[test]
    fn repository_fixtures_need_new_manifest_fields() {
        // The inherited fixtures predate "version" and "minVersion".
        let text = include_str!("../../../../fixtures/vehicles/republic-gunship/mod.json");
        let error = parse_manifest(text).unwrap_err();
        assert!(error.contains("version"), "{error}");
    }
}
