//! The local ZIP library: inventory derived from disk, and batch import.
//! See plans/mod-manager.md#packages-and-versions.

use std::collections::{BTreeMap, BTreeSet, HashMap, HashSet};
use std::fs;
use std::io;
use std::path::{Path, PathBuf};
use std::time::{Duration, SystemTime, UNIX_EPOCH};

use crate::package::{self, PackageInfo};
use crate::version::Version;

/// Files changed more recently than this may still be copying.
pub const SETTLE: Duration = Duration::from_secs(3);
const STAGING_PREFIX: &str = ".reomp-import-";

#[derive(Debug, Clone)]
pub struct VersionEntry {
    pub info: PackageInfo,
    /// Every ZIP claiming this ID and version, with its SHA-256.
    pub files: BTreeMap<PathBuf, String>,
}

impl VersionEntry {
    /// Different ZIPs claim the same ID and version.
    pub fn is_conflict(&self) -> bool {
        self.files.values().collect::<BTreeSet<_>>().len() > 1
    }
}

#[derive(Debug, Clone)]
pub struct InvalidFile {
    pub file: PathBuf,
    pub reason: String,
}

#[derive(Debug, Clone, Default)]
pub struct Library {
    pub packages: BTreeMap<String, BTreeMap<Version, VersionEntry>>,
    pub invalid: Vec<InvalidFile>,
    /// Some ZIP couldn't be read yet and was changed moments ago; scan again shortly.
    pub copying: bool,
}

impl Library {
    pub fn version(&self, id: &str, version: Version) -> Option<&VersionEntry> {
        self.packages.get(id)?.get(&version)
    }

    fn add(&mut self, file: PathBuf, info: PackageInfo) {
        let entry = self
            .packages
            .entry(info.id.clone())
            .or_default()
            .entry(info.manifest.version)
            .or_insert_with(|| VersionEntry { info: info.clone(), files: BTreeMap::new() });
        entry.files.insert(file, info.sha256);
    }
}

fn is_zip(path: &Path) -> bool {
    path.extension().is_some_and(|e| e.eq_ignore_ascii_case("zip"))
}

fn file_label(path: &Path) -> String {
    path.file_name().map_or_else(|| path.display().to_string(), |n| n.to_string_lossy().into_owned())
}

/// Builds the inventory from the ZIPs directly inside `dir`.
pub fn scan(dir: &Path) -> io::Result<Library> {
    let mut library = Library::default();
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        let path = entry.path();
        if entry.file_name().to_string_lossy().starts_with(STAGING_PREFIX) || !is_zip(&path) {
            continue;
        }
        let Ok(meta) = entry.metadata() else { continue };
        if !meta.is_file() {
            continue;
        }
        let recent = meta
            .modified()
            .ok()
            .and_then(|m| SystemTime::now().duration_since(m).ok())
            .is_some_and(|age| age < SETTLE);
        match package::read_package(&path) {
            Ok(info) => library.add(path, info),
            Err(_) if recent => library.copying = true,
            Err(reason) => library.invalid.push(InvalidFile { file: path, reason }),
        }
    }
    Ok(library)
}

#[derive(Debug, Default)]
pub struct ImportSummary {
    pub added: Vec<String>,
    pub skipped: Vec<String>,
}

fn unique_suffix() -> String {
    let nanos = SystemTime::now().duration_since(UNIX_EPOCH).map_or(0, |d| d.as_nanos());
    format!("{}-{nanos}", std::process::id())
}

/// Validates the whole batch first, then copies each ZIP into `dir` and
/// publishes it by rename. Nothing is published if any file is rejected.
pub fn import(dir: &Path, library: &Library, sources: &[PathBuf]) -> Result<ImportSummary, Vec<String>> {
    let mut errors = Vec::new();
    let mut summary = ImportSummary::default();
    let mut planned: Vec<(PathBuf, PackageInfo)> = Vec::new();
    let mut in_batch: HashMap<(String, Version), (String, String)> = HashMap::new();

    for source in sources {
        let label = file_label(source);
        if !is_zip(source) {
            errors.push(format!("{label}: not a .zip file"));
            continue;
        }
        let info = match package::read_package(source) {
            Ok(info) => info,
            Err(reason) => {
                errors.push(format!("{label}: {reason}"));
                continue;
            }
        };
        let version = info.manifest.version;
        let what = format!("{} {}", info.id, version);
        if let Some(existing) = library.version(&info.id, version) {
            if existing.files.values().all(|sha| *sha == info.sha256) {
                summary.skipped.push(format!("{what} is already in the library"));
            } else {
                let others: Vec<String> = existing.files.keys().map(|p| file_label(p)).collect();
                errors.push(format!(
                    "{label}: {what} is already in the library with different contents ({}). Give the new package a higher version.",
                    others.join(", ")
                ));
            }
            continue;
        }
        match in_batch.get(&(info.id.clone(), version)) {
            Some((sha, _)) if *sha == info.sha256 => {
                summary.skipped.push(format!("{label}: same package as another file in this batch"));
            }
            Some((_, other)) => {
                errors.push(format!("{label} and {other} both claim {what} with different contents"));
            }
            None => {
                in_batch.insert((info.id.clone(), version), (info.sha256.clone(), label));
                planned.push((source.clone(), info));
            }
        }
    }
    if !errors.is_empty() {
        return Err(errors);
    }

    // Stage beside the destination, then publish complete files by rename.
    let suffix = unique_suffix();
    let mut staged: Vec<(PathBuf, PathBuf, String)> = Vec::new();
    let mut taken: HashSet<String> = HashSet::new();
    let stage_result = (|| -> Result<(), String> {
        for (index, (source, info)) in planned.iter().enumerate() {
            let label = file_label(source);
            let temp = dir.join(format!("{STAGING_PREFIX}{suffix}-{index}.zip.part"));
            staged.push((temp.clone(), PathBuf::new(), label.clone()));
            fs::copy(source, &temp).map_err(|e| format!("{label}: couldn't copy into the library: {e}"))?;
            let copied = package::sha256_file(&temp).map_err(|e| format!("{label}: couldn't check the copy: {e}"))?;
            if copied != info.sha256 {
                return Err(format!("{label}: the file changed while it was being imported. Try again."));
            }
            let base = format!("{}-{}", info.id, info.manifest.version);
            let mut name = format!("{base}.zip");
            let mut n = 2;
            while dir.join(&name).exists() || !taken.insert(name.to_lowercase()) {
                name = format!("{base} ({n}).zip");
                n += 1;
            }
            staged.last_mut().unwrap().1 = dir.join(&name);
            summary.added.push(format!("{} {}", info.id, info.manifest.version));
        }
        Ok(())
    })();
    if let Err(error) = stage_result {
        for (temp, _, _) in &staged {
            let _ = fs::remove_file(temp);
        }
        return Err(vec![error]);
    }

    let mut published: Vec<PathBuf> = Vec::new();
    for (temp, dest, label) in &staged {
        if let Err(e) = fs::rename(temp, dest) {
            for path in &published {
                let _ = fs::remove_file(path);
            }
            for (temp, _, _) in &staged {
                let _ = fs::remove_file(temp);
            }
            return Err(vec![format!("{label}: couldn't add it to the library: {e}")]);
        }
        published.push(dest.clone());
    }
    Ok(summary)
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::{temp_dir, write_mod};

    fn zip_names(dir: &Path) -> Vec<String> {
        let mut names: Vec<String> = fs::read_dir(dir)
            .unwrap()
            .map(|e| e.unwrap().file_name().to_string_lossy().into_owned())
            .collect();
        names.sort();
        names
    }

    #[test]
    fn scan_groups_versions_and_detects_conflicts() {
        let dir = temp_dir("scan");
        write_mod(&dir, "a1.zip", "a", "1.0.0", &[], "x");
        write_mod(&dir, "a1-copy.zip", "a", "1.0.0", &[], "x");
        write_mod(&dir, "a2.zip", "a", "2.0.0", &[], "x");
        write_mod(&dir, "b1.zip", "b", "1.0.0", &[], "one");
        write_mod(&dir, "b1-other.zip", "b", "1.0.0", &[], "two");
        fs::write(dir.join("broken.zip"), b"not a zip").unwrap();
        crate::testutil::make_settled(&dir.join("broken.zip"));
        fs::write(dir.join("notes.txt"), b"ignored").unwrap();

        let library = scan(&dir).unwrap();
        let a = &library.packages["a"];
        assert_eq!(a.len(), 2);
        assert!(!a[&Version(1, 0, 0)].is_conflict(), "identical ZIPs are one package");
        assert!(library.packages["b"][&Version(1, 0, 0)].is_conflict());
        assert_eq!(library.invalid.len(), 1);
        assert!(!library.copying);
    }

    #[test]
    fn a_file_still_being_copied_is_not_reported_invalid() {
        let dir = temp_dir("copying");
        fs::write(dir.join("partial.zip"), b"PK\x03\x04 half").unwrap();
        let library = scan(&dir).unwrap();
        assert!(library.copying);
        assert!(library.invalid.is_empty());
    }

    #[test]
    fn import_publishes_a_valid_batch() {
        let source = temp_dir("import-src");
        let dir = temp_dir("import-lib");
        let a = write_mod(&source, "A.zip", "a", "1.0.0", &[], "x");
        let b = write_mod(&source, "B.zip", "b", "1.2.0", &[("a", "1.0.0")], "x");
        let summary = import(&dir, &Library::default(), &[a.clone(), b]).unwrap();
        assert_eq!(summary.added, vec!["a 1.0.0", "b 1.2.0"]);
        assert_eq!(zip_names(&dir), vec!["a-1.0.0.zip", "b-1.2.0.zip"]);

        // Re-importing the same file is skipped, not duplicated.
        let library = scan(&dir).unwrap();
        let again = import(&dir, &library, &[a]).unwrap();
        assert!(again.added.is_empty());
        assert_eq!(again.skipped.len(), 1);
    }

    #[test]
    fn import_rejects_the_whole_batch_when_one_file_is_bad() {
        let source = temp_dir("reject-src");
        let dir = temp_dir("reject-lib");
        let good = write_mod(&source, "good.zip", "good", "1.0.0", &[], "x");
        let bad = source.join("bad.zip");
        fs::write(&bad, b"nope").unwrap();
        let errors = import(&dir, &Library::default(), &[good, bad]).unwrap_err();
        assert_eq!(errors.len(), 1);
        assert!(errors[0].starts_with("bad.zip"));
        assert!(zip_names(&dir).is_empty(), "nothing is published");
    }

    #[test]
    fn import_rejects_same_version_with_different_bytes() {
        let source = temp_dir("dup-src");
        let dir = temp_dir("dup-lib");
        let first = write_mod(&source, "one.zip", "m", "1.0.0", &[], "one");
        import(&dir, &Library::default(), &[first]).unwrap();
        let second = write_mod(&source, "two.zip", "m", "1.0.0", &[], "two");
        let errors = import(&dir, &scan(&dir).unwrap(), &[second]).unwrap_err();
        assert!(errors[0].contains("different contents"), "{}", errors[0]);

        let x = write_mod(&source, "x.zip", "n", "1.0.0", &[], "x");
        let y = write_mod(&source, "y.zip", "n", "1.0.0", &[], "y");
        assert!(import(&dir, &scan(&dir).unwrap(), &[x, y]).is_err());
        assert_eq!(zip_names(&dir), vec!["m-1.0.0.zip"]);
    }
}
