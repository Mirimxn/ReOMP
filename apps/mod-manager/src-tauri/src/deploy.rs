//! Installs the OMP payload and the selected mods into game folders as one
//! transaction across targets, with a journal so an interrupted change can be
//! reversed. See plans/mod-manager.md#safe-apply-and-recovery.

use std::collections::{BTreeMap, BTreeSet};
use std::fs::{self, File, OpenOptions};
use std::io::{self, BufRead, BufReader, Read, Write};
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use crate::package;
use crate::version::Version;

pub const MODS_DIR: &str = "mods";
pub const LEGACY_DIR: &str = "legacy-mods";
const TXN_PREFIX: &str = ".reomp-txn-";
/// Loose game files a mod replaced, kept until the mod no longer needs the path.
pub const ORIGINALS_DIR: &str = ".reomp-originals";
/// Files only an OMP installation puts in the game folder.
const OMP_MARKERS: &[&str] = &["omp-lswtss-driver.dll", "omp-lswtss-driver-config.json"];
const OMP_PREFIX: &str = "omp-lswtss-";

// ---------------------------------------------------------------- payload

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub struct PayloadFile {
    pub path: String,
    pub sha256: String,
}

/// The OMP runtime files the manager installs into each game folder,
/// listed in `payload.json` beside them.
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub struct Payload {
    pub version: String,
    pub files: Vec<PayloadFile>,
    #[serde(skip)]
    pub root: PathBuf,
}

impl Payload {
    pub fn load(root: &Path) -> Result<Payload, String> {
        let list = root.join("payload.json");
        let text = fs::read_to_string(&list).map_err(|e| format!("Can't read {}: {e}", list.display()))?;
        let mut payload: Payload =
            serde_json::from_str(&text).map_err(|e| format!("{} isn't valid: {e}", list.display()))?;
        payload.root = root.to_path_buf();
        if payload.files.is_empty() {
            return Err(format!("{} lists no files", list.display()));
        }
        for file in &payload.files {
            check_rel(&file.path)?;
            let first = file.path.split('/').next().unwrap_or("");
            if first.eq_ignore_ascii_case(MODS_DIR) || first.eq_ignore_ascii_case(LEGACY_DIR) {
                return Err(format!("payload file {} is inside {first}/", file.path));
            }
            if !rel_path(root, &file.path).is_file() {
                return Err(format!("payload file {} is missing", file.path));
            }
        }
        Ok(payload)
    }

    fn top_level(&self) -> BTreeSet<String> {
        self.files.iter().map(|f| f.path.split('/').next().unwrap_or("").to_string()).collect()
    }
}

fn check_rel(rel: &str) -> Result<(), String> {
    let bad = rel.is_empty()
        || rel.contains('\\')
        || rel.contains(':')
        || rel.starts_with('/')
        || rel.split('/').any(|s| s.is_empty() || s == "." || s == "..");
    if bad { Err(format!("\"{rel}\" isn't a safe relative path")) } else { Ok(()) }
}

/// Folder in the game where files the player edited are kept when they choose the
/// Mod Manager's copies instead (hidden, so mods and the game never see it).
pub const EDITED_DIR: &str = ".reomp-edited";

/// Moves every file the manager installed that was changed since into
/// `.reomp-edited/<time>/` (same relative path), so the next update can put the
/// manager's copy back. Nothing is deleted. Returns the moved relative paths.
pub fn set_aside_edited(receipt: &Receipt) -> Result<Vec<String>, String> {
    let game = receipt.game_dir.as_path();
    let stamp = std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).map(|d| d.as_secs()).unwrap_or_default();
    let keep = game.join(EDITED_DIR).join(stamp.to_string());
    let mut moved = Vec::new();
    for (rel, sha) in &receipt.files {
        let path = rel_path(game, rel);
        if !path.is_file() {
            continue;
        }
        let actual = package::sha256_file(&path).map_err(|e| format!("Can't read {}: {e}", path.display()))?;
        if actual == *sha {
            continue;
        }
        let to = rel_path(&keep, rel);
        if let Some(parent) = to.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("Can't create {}: {e}", parent.display()))?;
        }
        fs::rename(&path, &to).map_err(|e| format!("Can't move {} aside: {e}", path.display()))?;
        moved.push(rel.clone());
    }
    Ok(moved)
}

fn rel_path(base: &Path, rel: &str) -> PathBuf {
    rel.split('/').fold(base.to_path_buf(), |path, segment| path.join(segment))
}

// ---------------------------------------------------------------- receipts

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReceiptMod {
    pub version: Version,
    pub zip_sha256: String,
    /// Game paths this mod gave up to another mod.
    #[serde(default)]
    pub skipped: BTreeSet<String>,
    /// SHA-256 of the files the manager wrote into this mod's folder (see `DeployMod::generated`).
    #[serde(default, skip_serializing_if = "String::is_empty")]
    pub generated: String,
}

/// What the manager installed into one game folder.
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Receipt {
    pub game_dir: PathBuf,
    pub payload_version: String,
    pub mods: BTreeMap<String, ReceiptMod>,
    /// Every owned file, relative with `/`, and its SHA-256.
    pub files: BTreeMap<String, String>,
    /// Earlier `mods/` folders moved aside, relative to the game folder.
    #[serde(default)]
    pub legacy_backups: Vec<String>,
    /// Game paths placed from mods' resource declarations, and the owning mod.
    #[serde(default)]
    pub resources: BTreeMap<String, String>,
    /// Game paths whose original loose file was set aside, and where it is.
    #[serde(default)]
    pub originals: BTreeMap<String, String>,
}

#[derive(Debug, Clone)]
pub struct DeployMod {
    pub id: String,
    pub version: Version,
    pub zip: PathBuf,
    pub zip_sha256: String,
    /// Game files this mod places, from its manifest.
    pub resources: Vec<package::Resource>,
    /// Declared game paths it gives up to a mod chosen over it. They aren't
    /// placed, and their actions are removed from its installed mod.json.
    pub skipped: BTreeSet<String>,
    /// Files the manager writes into the mod's folder after unpacking it, by
    /// path inside the folder (the combined background-character palette).
    pub generated: Vec<(String, Vec<u8>)>,
    /// SHA-256 over `generated`, empty when there is none.
    pub generated_sha: String,
}

/// One target's requested end state. `want: None` removes the manager's files.
pub struct TargetPlan<'a> {
    pub label: String,
    pub game_dir: PathBuf,
    pub want: Option<(&'a Payload, &'a [DeployMod])>,
    pub previous: Option<Receipt>,
}

impl TargetPlan<'_> {
    /// True when the folder already matches the request.
    pub fn is_satisfied(&self) -> bool {
        match (&self.want, &self.previous) {
            (None, None) => true,
            (Some((payload, mods)), Some(receipt)) => {
                receipt.payload_version == payload.version
                    && receipt.mods.len() == mods.len()
                    && mods.iter().all(|m| {
                        receipt.mods.get(&m.id).is_some_and(|r| {
                            r.zip_sha256 == m.zip_sha256
                                && r.version == m.version
                                && r.skipped == m.skipped
                                && r.generated == m.generated_sha
                        })
                    })
            }
            _ => false,
        }
    }
}

// ---------------------------------------------------------------- journal

#[derive(Debug, Serialize, Deserialize)]
#[serde(tag = "op", rename_all = "camelCase")]
enum Entry {
    Begin { id: String },
    Txn { dir: PathBuf },
    Mkdir { dir: PathBuf },
    Move { from: PathBuf, to: PathBuf },
    Commit { receipts: BTreeMap<String, Option<Receipt>> },
}

pub type Receipts = BTreeMap<String, Option<Receipt>>;

/// An in-progress change. Every filesystem step is journalled before it runs.
pub struct Deployment {
    journal: PathBuf,
    log: File,
    id: String,
    moves: Vec<(PathBuf, PathBuf)>,
    created: Vec<PathBuf>,
    txn_dirs: Vec<PathBuf>,
    /// Folders to remove after commit if left empty, with the game folder to stop at.
    tidy: Vec<(PathBuf, PathBuf)>,
    #[cfg(test)]
    pub fail_at_move: Option<usize>,
    /// Folders that behave as if another program holds them open.
    #[cfg(test)]
    pub locked_dirs: Vec<PathBuf>,
}

impl Deployment {
    pub fn begin(journal: &Path) -> Result<Deployment, String> {
        if journal.exists() {
            return Err("An earlier change hasn't been recovered yet.".into());
        }
        if let Some(parent) = journal.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("Can't create {}: {e}", parent.display()))?;
        }
        let log = OpenOptions::new()
            .create_new(true)
            .append(true)
            .open(journal)
            .map_err(|e| format!("Can't create {}: {e}", journal.display()))?;
        let nanos = SystemTime::now().duration_since(UNIX_EPOCH).map_or(0, |d| d.as_nanos());
        let mut deployment = Deployment {
            journal: journal.to_path_buf(),
            log,
            id: format!("{}-{nanos}", std::process::id()),
            moves: Vec::new(),
            created: Vec::new(),
            txn_dirs: Vec::new(),
            tidy: Vec::new(),
            #[cfg(test)]
            fail_at_move: None,
            #[cfg(test)]
            locked_dirs: Vec::new(),
        };
        let id = deployment.id.clone();
        deployment.write(&Entry::Begin { id })?;
        Ok(deployment)
    }

    fn write(&mut self, entry: &Entry) -> Result<(), String> {
        let mut line = serde_json::to_string(entry).map_err(|e| e.to_string())?;
        line.push('\n');
        self.log
            .write_all(line.as_bytes())
            .and_then(|_| self.log.sync_data())
            .map_err(|e| format!("Can't write the change journal {}: {e}", self.journal.display()))
    }

    fn mkdir_all(&mut self, dir: &Path) -> Result<(), String> {
        let mut missing = Vec::new();
        let mut current = Some(dir);
        while let Some(path) = current {
            if path.exists() {
                break;
            }
            missing.push(path.to_path_buf());
            current = path.parent();
        }
        for path in missing.into_iter().rev() {
            self.write(&Entry::Mkdir { dir: path.clone() })?;
            fs::create_dir(&path).map_err(|e| format!("Can't create {}: {e}", path.display()))?;
            self.created.push(path);
        }
        Ok(())
    }

    fn move_path(&mut self, from: &Path, to: &Path) -> Result<(), String> {
        if let Some(parent) = to.parent() {
            self.mkdir_all(parent)?;
        }
        #[cfg(test)]
        if self.fail_at_move == Some(self.moves.len()) {
            return Err(format!("Injected failure moving {}", from.display()));
        }
        let is_dir = from.is_dir();
        self.write(&Entry::Move { from: from.to_path_buf(), to: to.to_path_buf() })?;
        // A folder that another program watches (File Explorer does this for
        // folders it has shown) can't be renamed, but its contents can be moved.
        let attempts = if is_dir { 1 } else { 7 };
        #[cfg(test)]
        let result = if self.locked_dirs.iter().any(|d| d == from) {
            Err(io::Error::from(io::ErrorKind::PermissionDenied))
        } else {
            rename_with_retry(from, to, attempts)
        };
        #[cfg(not(test))]
        let result = rename_with_retry(from, to, attempts);
        match result {
            Ok(()) => {
                self.moves.push((from.to_path_buf(), to.to_path_buf()));
                Ok(())
            }
            Err(e) if is_dir && is_in_use(&e) => self.move_children(from, to),
            Err(e) => Err(describe_move_error(from, to, &e)),
        }
    }

    /// Moves a folder's contents one by one and leaves the emptied folder.
    /// Undo moves them back, recreating the folder if it was removed.
    fn move_children(&mut self, from: &Path, to: &Path) -> Result<(), String> {
        self.mkdir_all(to)?;
        let mut children: Vec<PathBuf> = fs::read_dir(from)
            .map_err(|e| format!("Can't read {}: {e}", from.display()))?
            .flatten()
            .map(|e| e.path())
            .collect();
        children.sort();
        for child in children {
            let name = child.file_name().map(|n| n.to_os_string()).unwrap_or_default();
            self.move_path(&child, &to.join(name))?;
        }
        let _ = fs::remove_dir(from);
        Ok(())
    }

    /// Brings one game folder to the plan's end state. Returns its new receipt.
    pub fn apply(&mut self, plan: &TargetPlan) -> Result<Option<Receipt>, String> {
        let game = plan.game_dir.as_path();
        let label = &plan.label;
        let previous_files = plan.previous.as_ref().map(|r| r.files.clone()).unwrap_or_default();

        // 1. Stop if anything the manager installed was changed by someone else.
        for (rel, sha) in &previous_files {
            let path = rel_path(game, rel);
            if path.is_file() {
                let actual = package::sha256_file(&path).map_err(|e| format!("{label}: can't read {}: {e}", path.display()))?;
                if actual != *sha {
                    return Err(format!(
                        "{label}: {} was changed outside the Mod Manager. Move it somewhere else or restore it, then press Retry.",
                        path.display()
                    ));
                }
            }
        }

        let txn = game.join(format!("{TXN_PREFIX}{}", self.id));
        let stage = txn.join("stage");
        let backup = txn.join("backup");
        self.write(&Entry::Txn { dir: txn.clone() })?;
        self.txn_dirs.push(txn.clone());
        self.mkdir_all(&stage)?;

        // 2. Work out the files the folder should end up with.
        let mut desired: BTreeMap<String, String> = BTreeMap::new();
        let mut staged: BTreeSet<String> = BTreeSet::new();
        let mut receipt_mods = BTreeMap::new();
        let mut resource_owner: BTreeMap<String, String> = BTreeMap::new();
        let previous_resources = plan.previous.as_ref().map(|r| r.resources.clone()).unwrap_or_default();
        let mut originals = plan.previous.as_ref().map(|r| r.originals.clone()).unwrap_or_default();
        if let Some((payload, mods)) = plan.want {
            for file in &payload.files {
                desired.insert(file.path.clone(), file.sha256.clone());
            }
            for m in mods {
                receipt_mods.insert(
                    m.id.clone(),
                    ReceiptMod {
                        version: m.version,
                        zip_sha256: m.zip_sha256.clone(),
                        skipped: m.skipped.clone(),
                        generated: m.generated_sha.clone(),
                    },
                );
                let placing: Vec<&package::Resource> = m.resources.iter().filter(|r| !m.skipped.contains(&r.path)).collect();
                let prefix = format!("{MODS_DIR}/{}/", m.id);
                let previous_mod: Vec<(&String, &String)> =
                    previous_files.iter().filter(|(rel, _)| rel.starts_with(&prefix)).collect();
                let previous_placed: Vec<&String> =
                    previous_resources.iter().filter(|(_, owner)| **owner == m.id).map(|(path, _)| path).collect();
                let unchanged = plan
                    .previous
                    .as_ref()
                    .and_then(|r| r.mods.get(&m.id))
                    .is_some_and(|r| r.zip_sha256 == m.zip_sha256 && r.skipped == m.skipped && r.generated == m.generated_sha)
                    && previous_mod.iter().all(|(rel, _)| rel_path(game, rel).is_file())
                    && previous_placed.len() == placing.len()
                    && previous_placed.iter().all(|rel| rel_path(game, rel).is_file() && previous_files.contains_key(*rel));
                if unchanged {
                    for (rel, sha) in previous_mod {
                        desired.insert(rel.clone(), sha.clone());
                    }
                    for rel in previous_placed {
                        desired.insert(rel.clone(), previous_files[rel].clone());
                        resource_owner.insert(rel.clone(), m.id.clone());
                    }
                } else {
                    let extracted = extract_mod(m, &stage).map_err(|e| format!("{label}: {e}"))?;
                    for resource in placing {
                        let wanted = format!("{MODS_DIR}/{}/{}", m.id, resource.src).to_lowercase();
                        let Some((source_rel, _)) = extracted.iter().find(|(rel, _)| rel.to_lowercase() == wanted) else {
                            return Err(format!("{label}: {} points {} at {}, which isn't in the package", m.id, resource.path, resource.src));
                        };
                        if let Some(other) = resource_owner.get(&resource.path) {
                            return Err(format!("{label}: {other} and {} both replace {}", m.id, resource.path));
                        }
                        let target = rel_path(&stage, &resource.path);
                        self.mkdir_all(target.parent().unwrap_or(&stage))?;
                        let mut source = File::open(rel_path(&stage, source_rel)).map_err(|e| format!("{label}: {e}"))?;
                        let sha = copy_hashed(&mut source, &target).map_err(|e| format!("{label}: can't stage {}: {e}", resource.path))?;
                        staged.insert(resource.path.clone());
                        desired.insert(resource.path.clone(), sha);
                        resource_owner.insert(resource.path.clone(), m.id.clone());
                    }
                    for (rel, sha) in extracted {
                        staged.insert(rel.clone());
                        desired.insert(rel, sha);
                    }
                }
            }
        }

        // 3. Check every destination before changing anything.
        let first_install = plan.previous.is_none() && plan.want.is_some();
        let mut legacy_items: Vec<String> = Vec::new();
        if first_install {
            let payload = plan.want.map(|(p, _)| p).expect("first install has a payload");
            let has_omp = OMP_MARKERS.iter().any(|m| game.join(m).is_file());
            let entries: Vec<String> = fs::read_dir(game)
                .map_err(|e| format!("{label}: can't read {}: {e}", game.display()))?
                .flatten()
                .map(|e| e.file_name().to_string_lossy().into_owned())
                .collect();
            for top in payload.top_level() {
                if let Some(existing) = entries.iter().find(|e| e.eq_ignore_ascii_case(&top)) {
                    if !has_omp {
                        return Err(format!(
                            "{label}: {} already exists and doesn't belong to OMP. Move it out of the game folder, then press Retry.",
                            game.join(existing).display()
                        ));
                    }
                    legacy_items.push(existing.clone());
                }
            }
            if has_omp {
                // Older OMP versions use versioned folder names.
                for entry in &entries {
                    let is_old_omp = entry.to_ascii_lowercase().starts_with(OMP_PREFIX)
                        || entry.eq_ignore_ascii_case("dinput8.dll");
                    if is_old_omp && !legacy_items.iter().any(|l| l.eq_ignore_ascii_case(entry)) {
                        legacy_items.push(entry.clone());
                    }
                }
            }
        }
        // Loose game files at a resource path: identical ones (left by an
        // earlier runtime link) are replaced; others are set aside and restored later.
        let mut set_aside: Vec<String> = Vec::new();
        let mut replace_identical: Vec<String> = Vec::new();
        for (rel, sha) in &desired {
            let path = rel_path(game, rel);
            let top = rel.split('/').next().unwrap_or("");
            let replaced_legacy = legacy_items.iter().any(|l| l.eq_ignore_ascii_case(top));
            let owned = previous_files.contains_key(rel);
            let in_old_mods = first_install && top.eq_ignore_ascii_case(MODS_DIR);
            if !path.exists() || owned || replaced_legacy || in_old_mods {
                continue;
            }
            if resource_owner.contains_key(rel) && path.is_file() {
                let existing = package::sha256_file(&path).map_err(|e| format!("{label}: can't read {}: {e}", path.display()))?;
                if existing == *sha {
                    replace_identical.push(rel.clone());
                } else {
                    set_aside.push(rel.clone());
                }
                continue;
            }
            return Err(format!(
                "{label}: {} already exists and wasn't installed by the Mod Manager. Move it out of the game folder, then press Retry.",
                path.display()
            ));
        }

        // 4. Move aside what is being replaced.
        let mut legacy_backups = plan.previous.as_ref().map(|r| r.legacy_backups.clone()).unwrap_or_default();
        if first_install {
            for item in &legacy_items {
                self.move_path(&game.join(item), &backup.join(item))?;
            }
            let old_mods = game.join(MODS_DIR);
            if old_mods.exists() {
                let nanos = SystemTime::now().duration_since(UNIX_EPOCH).map_or(0, |d| d.as_secs());
                let mut name = format!("mods-before-reomp-{nanos}");
                let mut n = 2;
                while game.join(LEGACY_DIR).join(&name).exists() {
                    name = format!("mods-before-reomp-{nanos}-{n}");
                    n += 1;
                }
                self.move_path(&old_mods, &game.join(LEGACY_DIR).join(&name))?;
                legacy_backups.push(format!("{LEGACY_DIR}/{name}"));
            }
        }
        for (rel, sha) in &previous_files {
            if desired.get(rel) != Some(sha) || staged.contains(rel) {
                let path = rel_path(game, rel);
                if path.exists() {
                    self.move_path(&path, &rel_path(&backup, rel))?;
                }
                if !desired.contains_key(rel) {
                    if let Some(kept) = originals.remove(rel) {
                        self.move_path(&rel_path(game, &kept), &path)?;
                        if let Some(parent) = rel_path(game, &kept).parent() {
                            self.tidy.push((parent.to_path_buf(), game.to_path_buf()));
                        }
                    } else if let Some(parent) = path.parent() {
                        self.tidy.push((parent.to_path_buf(), game.to_path_buf()));
                    }
                }
            }
        }
        for rel in &replace_identical {
            self.move_path(&rel_path(game, rel), &rel_path(&backup, rel))?;
        }
        for rel in &set_aside {
            let kept = format!("{ORIGINALS_DIR}/{rel}");
            self.move_path(&rel_path(game, rel), &rel_path(game, &kept))?;
            originals.insert(rel.clone(), kept);
        }

        // 5. Stage changed payload files, then publish everything staged.
        if let Some((payload, _)) = plan.want {
            for file in &payload.files {
                let unchanged = previous_files.get(&file.path) == Some(&file.sha256) && rel_path(game, &file.path).is_file();
                if unchanged {
                    continue;
                }
                let target = rel_path(&stage, &file.path);
                self.mkdir_all(target.parent().unwrap_or(&stage))?;
                let copied = copy_hashed(&mut File::open(rel_path(&payload.root, &file.path)).map_err(|e| e.to_string())?, &target)
                    .map_err(|e| format!("{label}: can't stage {}: {e}", file.path))?;
                if copied != file.sha256 {
                    return Err(format!("{label}: the bundled OMP file {} is damaged. Reinstall the Mod Manager.", file.path));
                }
                staged.insert(file.path.clone());
            }
        }
        for rel in &staged {
            self.move_path(&rel_path(&stage, rel), &rel_path(game, rel))?;
        }

        Ok(plan.want.map(|(payload, _)| Receipt {
            game_dir: plan.game_dir.clone(),
            payload_version: payload.version.clone(),
            mods: receipt_mods,
            files: desired,
            legacy_backups,
            resources: resource_owner,
            originals,
        }))
    }

    /// Records success, saves receipts, then deletes rollback data.
    pub fn commit(mut self, receipts: Receipts, save: impl FnOnce(&Receipts) -> Result<(), String>) -> Result<(), String> {
        self.write(&Entry::Commit { receipts: receipts.clone() })?;
        save(&receipts)?;
        finish(&self.txn_dirs, &self.tidy);
        drop(self.log);
        fs::remove_file(&self.journal).map_err(|e| format!("Can't remove {}: {e}", self.journal.display()))
    }

    /// Reverses every step taken so far.
    pub fn rollback(self) -> Result<(), String> {
        drop(self.log);
        undo(&self.moves, &self.created, &self.txn_dirs)?;
        fs::remove_file(&self.journal).map_err(|e| format!("Can't remove {}: {e}", self.journal.display()))
    }
}

/// Windows refuses to rename a folder while any program has a file inside it
/// open (error 5 or 32). Antivirus scans release them quickly, so retry briefly.
fn rename_with_retry(from: &Path, to: &Path, attempts: u64) -> io::Result<()> {
    let mut attempt: u64 = 1;
    loop {
        match fs::rename(from, to) {
            Err(e) if attempt < attempts && is_in_use(&e) => {
                std::thread::sleep(std::time::Duration::from_millis(250 * attempt));
                attempt += 1;
            }
            result => return result,
        }
    }
}

fn describe_move_error(from: &Path, to: &Path, error: &io::Error) -> String {
    if is_in_use(error) {
        format!(
            "Windows wouldn't let the Mod Manager move {}. Another program has it open. Close that program (for example a modding tool or a text editor), then press Retry. ({error})",
            from.display()
        )
    } else {
        format!("Can't move {} to {}: {error}", from.display(), to.display())
    }
}

fn is_in_use(error: &io::Error) -> bool {
    error.kind() == io::ErrorKind::PermissionDenied || matches!(error.raw_os_error(), Some(5) | Some(32))
}

fn finish(txn_dirs: &[PathBuf], tidy: &[(PathBuf, PathBuf)]) {
    for dir in txn_dirs {
        let _ = fs::remove_dir_all(dir);
    }
    for (dir, game) in tidy {
        remove_empty_dirs(dir, game);
    }
}

/// Removes `dir` and its parents while they are empty, stopping at the game
/// folder and its `mods/` folder.
fn remove_empty_dirs(dir: &Path, game: &Path) {
    let keep = game.join(MODS_DIR);
    let mut current = Some(dir);
    while let Some(path) = current {
        if !path.starts_with(game) || path == game || path == keep || fs::remove_dir(path).is_err() {
            break;
        }
        current = path.parent();
    }
}

fn undo(moves: &[(PathBuf, PathBuf)], created: &[PathBuf], txn_dirs: &[PathBuf]) -> Result<(), String> {
    let mut failures = Vec::new();
    for (from, to) in moves.iter().rev() {
        if to.exists() && !from.exists() {
            if let Some(parent) = from.parent() {
                let _ = fs::create_dir_all(parent);
            }
            if let Err(e) = fs::rename(to, from) {
                failures.push(format!("{} → {}: {e}", to.display(), from.display()));
            }
        }
    }
    if !failures.is_empty() {
        return Err(format!("Couldn't undo every change: {}", failures.join("; ")));
    }
    for dir in txn_dirs {
        let _ = fs::remove_dir_all(dir);
    }
    for dir in created.iter().rev() {
        let _ = fs::remove_dir(dir);
    }
    Ok(())
}

/// Finishes or reverses a change interrupted by a crash or power loss.
/// Calls `save` with the committed receipts when the change had committed.
pub fn recover(journal: &Path, save: impl FnOnce(&Receipts) -> Result<(), String>) -> Result<bool, String> {
    let file = match File::open(journal) {
        Ok(file) => file,
        Err(e) if e.kind() == io::ErrorKind::NotFound => return Ok(false),
        Err(e) => return Err(format!("Can't read {}: {e}", journal.display())),
    };
    let mut moves = Vec::new();
    let mut created = Vec::new();
    let mut txn_dirs = Vec::new();
    let mut committed = None;
    for line in BufReader::new(file).lines() {
        let Ok(line) = line else { break };
        // A torn final line means that step never started.
        let Ok(entry) = serde_json::from_str::<Entry>(&line) else { break };
        match entry {
            Entry::Begin { .. } => {}
            Entry::Txn { dir } => txn_dirs.push(dir),
            Entry::Mkdir { dir } => created.push(dir),
            Entry::Move { from, to } => moves.push((from, to)),
            Entry::Commit { receipts } => committed = Some(receipts),
        }
    }
    match committed {
        Some(receipts) => {
            save(&receipts)?;
            finish(&txn_dirs, &[]);
        }
        None => undo(&moves, &created, &txn_dirs)?,
    }
    fs::remove_file(journal).map_err(|e| format!("Can't remove {}: {e}", journal.display()))?;
    Ok(true)
}

// ---------------------------------------------------------------- extraction

fn copy_hashed(reader: &mut impl Read, target: &Path) -> io::Result<String> {
    let mut out = File::create(target)?;
    let mut hasher = Sha256::new();
    let mut buffer = vec![0u8; 256 * 1024];
    loop {
        let n = reader.read(&mut buffer)?;
        if n == 0 {
            break;
        }
        hasher.update(&buffer[..n]);
        out.write_all(&buffer[..n])?;
    }
    out.sync_all()?;
    Ok(hasher.finalize().iter().map(|b| format!("{b:02x}")).collect())
}

/// The manifest without the resource actions for `skipped` paths, so the
/// runtime doesn't link files another mod was chosen for.
fn without_resources(text: &str, skipped: &BTreeSet<String>) -> Result<String, String> {
    let mut value: serde_json::Value = serde_json::from_str(text).map_err(|e| format!("mod.json isn't valid JSON: {e}"))?;
    if let Some(actions) = value.get_mut("actions").and_then(|a| a.as_array_mut()) {
        actions.retain(|action| {
            let type_id = action.get("typeId").and_then(|t| t.as_str()).unwrap_or("");
            let path = action.pointer("/payload/path").and_then(|p| p.as_str()).unwrap_or("");
            let given_up = package::RESOURCE_ACTIONS.contains(&type_id)
                && package::check_game_path(path).is_ok_and(|p| skipped.contains(&p));
            !given_up
        });
    }
    serde_json::to_string_pretty(&value).map_err(|e| e.to_string())
}

/// Unpacks a mod ZIP to `<stage>/mods/<id>/` and returns each file with its hash.
fn extract_mod(m: &DeployMod, stage: &Path) -> Result<Vec<(String, String)>, String> {
    let info = package::read_package(&m.zip).map_err(|e| format!("{}: {e}", m.zip.display()))?;
    if info.id != m.id || info.sha256 != m.zip_sha256 {
        return Err(format!("{} changed since it was checked. Press Retry.", m.zip.display()));
    }
    let file = File::open(&m.zip).map_err(|e| format!("Can't open {}: {e}", m.zip.display()))?;
    let mut archive = zip::ZipArchive::new(file).map_err(|e| format!("Can't read {}: {e}", m.zip.display()))?;
    let mut files = Vec::new();
    for index in 0..archive.len() {
        let mut entry = archive.by_index(index).map_err(|e| format!("Can't read {}: {e}", m.zip.display()))?;
        if entry.is_dir() {
            continue;
        }
        // read_package already rejected unsafe names.
        let rel = format!("{MODS_DIR}/{}", entry.name());
        let target = rel_path(stage, &rel);
        if let Some(parent) = target.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("Can't create {}: {e}", parent.display()))?;
        }
        let sha = if !m.skipped.is_empty() && entry.name() == format!("{}/mod.json", m.id) {
            let mut text = String::new();
            entry.read_to_string(&mut text).map_err(|e| format!("Can't read {rel}: {e}"))?;
            let trimmed = without_resources(&text, &m.skipped)?;
            copy_hashed(&mut trimmed.as_bytes(), &target).map_err(|e| format!("Can't unpack {}: {e}", rel))?
        } else {
            copy_hashed(&mut entry, &target).map_err(|e| format!("Can't unpack {}: {e}", rel))?
        };
        files.push((rel, sha));
    }
    for (inside, bytes) in &m.generated {
        let rel = format!("{MODS_DIR}/{}/{inside}", m.id);
        let target = rel_path(stage, &rel);
        if let Some(parent) = target.parent() {
            fs::create_dir_all(parent).map_err(|e| format!("Can't create {}: {e}", parent.display()))?;
        }
        let sha = copy_hashed(&mut bytes.as_slice(), &target).map_err(|e| format!("Can't write {rel}: {e}"))?;
        files.retain(|(r, _)| !r.eq_ignore_ascii_case(&rel));
        files.push((rel, sha));
    }
    Ok(files)
}

/// The SHA-256 that stands for a mod's generated files.
pub fn generated_sha(files: &[(String, Vec<u8>)]) -> String {
    use sha2::{Digest, Sha256};
    if files.is_empty() {
        return String::new();
    }
    let mut hash = Sha256::new();
    for (path, bytes) in files {
        hash.update(path.as_bytes());
        hash.update([0]);
        hash.update((bytes.len() as u64).to_le_bytes());
        hash.update(bytes);
    }
    format!("{:x}", hash.finalize())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::{temp_dir, write_mod};

    struct Fixture {
        root: PathBuf,
        payload: Payload,
        journal: PathBuf,
    }

    fn payload(root: &Path, version: &str, driver: &str) -> Payload {
        let dir = root.join(format!("payload-{version}"));
        let files = [
            ("dinput8.dll", "proxy".to_string()),
            ("omp-lswtss-driver.dll", driver.to_string()),
            ("omp-lswtss-runtime-engine-0.0.1/engine.dll", "engine".to_string()),
        ];
        let mut listed = Vec::new();
        for (rel, contents) in files {
            let path = rel_path(&dir, rel);
            fs::create_dir_all(path.parent().unwrap()).unwrap();
            fs::write(&path, &contents).unwrap();
            listed.push(PayloadFile { path: rel.into(), sha256: package::sha256_file(&path).unwrap() });
        }
        let list = Payload { version: version.into(), files: listed, root: PathBuf::new() };
        fs::write(dir.join("payload.json"), serde_json::to_string(&list).unwrap()).unwrap();
        Payload::load(&dir).unwrap()
    }

    fn fixture(label: &str) -> Fixture {
        let root = temp_dir(label);
        Fixture { payload: payload(&root, "1", "driver-1"), journal: root.join("journal.log"), root }
    }

    fn game(root: &Path, name: &str) -> PathBuf {
        let dir = root.join(name);
        fs::create_dir_all(&dir).unwrap();
        fs::write(dir.join(crate::targets::GAME_EXE), b"exe").unwrap();
        dir
    }

    fn deploy_mod(root: &Path, id: &str, version: &str, extra: &str) -> DeployMod {
        let zips = root.join("zips");
        fs::create_dir_all(&zips).unwrap();
        let zip = write_mod(&zips, &format!("{id}-{version}-{extra}.zip"), id, version, &[], extra);
        DeployMod {
            resources: Vec::new(),
            skipped: BTreeSet::new(),
            generated: Vec::new(),
            generated_sha: String::new(),
            id: id.into(),
            version: Version::parse(version).unwrap(),
            zip_sha256: package::sha256_file(&zip).unwrap(),
            zip,
        }
    }

    fn plan<'a>(dir: &Path, want: Option<(&'a Payload, &'a [DeployMod])>, previous: Option<Receipt>) -> TargetPlan<'a> {
        TargetPlan { label: "Test".into(), game_dir: dir.into(), want, previous }
    }

    fn run(f: &Fixture, plans: &[TargetPlan]) -> Result<Vec<Option<Receipt>>, String> {
        let mut d = Deployment::begin(&f.journal)?;
        let mut out = Vec::new();
        for p in plans {
            match d.apply(p) {
                Ok(r) => out.push(r),
                Err(e) => {
                    d.rollback()?;
                    return Err(e);
                }
            }
        }
        d.commit(BTreeMap::new(), |_| Ok(()))?;
        Ok(out)
    }

    fn snapshot(dir: &Path) -> BTreeMap<String, Vec<u8>> {
        fn walk(base: &Path, dir: &Path, out: &mut BTreeMap<String, Vec<u8>>) {
            for e in fs::read_dir(dir).unwrap().flatten() {
                let p = e.path();
                let rel = p.strip_prefix(base).unwrap().to_string_lossy().replace('\\', "/");
                if p.is_dir() {
                    out.insert(format!("{rel}/"), Vec::new());
                    walk(base, &p, out);
                } else {
                    out.insert(rel, fs::read(&p).unwrap());
                }
            }
        }
        let mut out = BTreeMap::new();
        walk(dir, dir, &mut out);
        out
    }

    #[test]
    fn installs_the_same_selection_on_two_targets_and_removes_it() {
        let f = fixture("deploy-two");
        let (steam, epic) = (game(&f.root, "steam"), game(&f.root, "epic"));
        let mods = vec![deploy_mod(&f.root, "a", "1.0.0", "x")];
        let r = run(&f, &[plan(&steam, Some((&f.payload, &mods)), None), plan(&epic, Some((&f.payload, &mods)), None)]).unwrap();
        for dir in [&steam, &epic] {
            assert_eq!(fs::read(dir.join("omp-lswtss-driver.dll")).unwrap(), b"driver-1");
            assert!(dir.join("mods/a/mod.json").is_file());
            assert!(!fs::read_dir(dir).unwrap().flatten().any(|e| e.file_name().to_string_lossy().starts_with(TXN_PREFIX)));
        }
        let steam_receipt = r[0].clone().unwrap();
        assert!(plan(&steam, Some((&f.payload, &mods)), Some(steam_receipt.clone())).is_satisfied());

        // Turning Steam off removes only owned files and leaves Epic alone.
        fs::write(steam.join("mods/notes.txt"), b"mine").unwrap();
        run(&f, &[plan(&steam, None, Some(steam_receipt))]).unwrap();
        assert!(!steam.join("dinput8.dll").exists());
        assert!(!steam.join("mods/a").exists());
        assert!(steam.join("mods/notes.txt").is_file());
        assert!(steam.join(crate::targets::GAME_EXE).is_file());
        assert!(epic.join("mods/a/mod.json").is_file());
    }

    #[test]
    fn replaces_existing_omp_and_keeps_old_mods_as_a_backup() {
        let f = fixture("deploy-legacy");
        let dir = game(&f.root, "game");
        fs::write(dir.join("dinput8.dll"), b"old proxy").unwrap();
        fs::write(dir.join("omp-lswtss-driver.dll"), b"old driver").unwrap();
        fs::create_dir_all(dir.join("omp-lswtss-runtime-engine-0.0.0")).unwrap();
        fs::create_dir_all(dir.join("mods/gunship")).unwrap();
        fs::write(dir.join("mods/gunship/mod.json"), b"{}").unwrap();
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &[])), None)]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("dinput8.dll")).unwrap(), b"proxy");
        assert!(!dir.join("omp-lswtss-runtime-engine-0.0.0").exists(), "older OMP folders are replaced");
        assert_eq!(receipt.legacy_backups.len(), 1);
        assert!(dir.join(&receipt.legacy_backups[0]).join("gunship/mod.json").is_file());
    }

    #[test]
    fn moves_a_held_mods_folder_by_its_contents() {
        let f = fixture("deploy-held");
        let dir = game(&f.root, "game");
        fs::write(dir.join("dinput8.dll"), b"old proxy").unwrap();
        fs::write(dir.join("omp-lswtss-driver.dll"), b"old driver").unwrap();
        fs::create_dir_all(dir.join("mods/gunship/icons")).unwrap();
        fs::write(dir.join("mods/gunship/mod.json"), b"{}").unwrap();
        fs::write(dir.join("mods/gunship/icons/i.png"), b"png").unwrap();
        let before = snapshot(&dir);

        // A failure after the contents moved rolls back to the same files.
        let mods = vec![deploy_mod(&f.root, "a", "1.0.0", "x")];
        let mut d = Deployment::begin(&f.journal).unwrap();
        d.locked_dirs = vec![dir.join("mods")];
        d.fail_at_move = Some(6);
        assert!(d.apply(&plan(&dir, Some((&f.payload, &mods)), None)).is_err());
        d.rollback().unwrap();
        assert_eq!(snapshot(&dir), before);

        let mut d = Deployment::begin(&f.journal).unwrap();
        d.locked_dirs = vec![dir.join("mods")];
        let receipt = d.apply(&plan(&dir, Some((&f.payload, &mods)), None)).unwrap().unwrap();
        d.commit(BTreeMap::new(), |_| Ok(())).unwrap();
        let backup = dir.join(&receipt.legacy_backups[0]);
        assert!(backup.join("gunship/icons/i.png").is_file());
        assert!(dir.join("mods/a/mod.json").is_file());
        assert!(!dir.join("mods/gunship").exists());
    }

    /// Holds a real Windows handle on the folder, like File Explorer does.
    #[cfg(windows)]
    #[test]
    fn moves_a_mods_folder_another_program_holds_open() {
        use std::os::windows::fs::OpenOptionsExt;
        const FILE_FLAG_BACKUP_SEMANTICS: u32 = 0x0200_0000;
        const FILE_SHARE_READ_WRITE: u32 = 0x1 | 0x2;
        let f = fixture("deploy-held-windows");
        let dir = game(&f.root, "game");
        fs::write(dir.join("omp-lswtss-driver.dll"), b"old driver").unwrap();
        fs::create_dir_all(dir.join("mods/gunship")).unwrap();
        fs::write(dir.join("mods/gunship/mod.json"), b"{}").unwrap();
        let _held = fs::OpenOptions::new()
            .read(true)
            .share_mode(FILE_SHARE_READ_WRITE)
            .custom_flags(FILE_FLAG_BACKUP_SEMANTICS)
            .open(dir.join("mods"))
            .unwrap();
        assert!(fs::rename(dir.join("mods"), dir.join("renamed")).is_err(), "the handle blocks a rename");

        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &[])), None)]).unwrap().remove(0).unwrap();
        assert!(dir.join(&receipt.legacy_backups[0]).join("gunship/mod.json").is_file());
    }

    fn resource_mod(root: &Path, id: &str, files: &[(&str, &[u8])]) -> DeployMod {
        let zips = root.join("zips");
        fs::create_dir_all(&zips).unwrap();
        let actions: Vec<String> = files
            .iter()
            .enumerate()
            .map(|(i, (path, _))| {
                format!(r#"{{"typeId":"v1/register-custom-resource-action","payload":{{"path":"{path}","srcPath":"files/{i}.bin"}}}}"#)
            })
            .collect();
        let json = format!(r#"{{"name":"{id}","version":"1.0.0","dependencies":[],"actions":[{}]}}"#, actions.join(","));
        let names: Vec<String> = (0..files.len()).map(|i| format!("{id}/files/{i}.bin")).collect();
        let manifest = format!("{id}/mod.json");
        let mut entries = vec![crate::testutil::Entry::File(&manifest, json.as_bytes())];
        for (name, (_, bytes)) in names.iter().zip(files) {
            entries.push(crate::testutil::Entry::File(name, bytes));
        }
        let zip = zips.join(format!("{id}.zip"));
        crate::testutil::write_zip(&zip, &entries);
        let info = package::read_package(&zip).unwrap();
        DeployMod {
            id: id.into(),
            version: info.manifest.version,
            zip_sha256: info.sha256,
            resources: info.manifest.resources,
            skipped: BTreeSet::new(),
            generated: Vec::new(),
            generated_sha: String::new(),
            zip,
        }
    }

    #[test]
    fn places_a_generated_file_and_replaces_it_when_it_changes() {
        let f = fixture("deploy-generated");
        let dir = game(&f.root, "game");
        let with = |bytes: &[u8]| {
            let mut m = resource_mod(&f.root, "population-palette", &[]);
            m.generated = vec![("merged/P.PRJ".to_string(), bytes.to_vec())];
            m.generated_sha = generated_sha(&m.generated);
            m.resources.push(package::Resource { path: "SYSTEMS/P.PRJ".into(), src: "merged/P.PRJ".into() });
            m
        };
        let mods = vec![with(b"one")];
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &mods)), None)]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("SYSTEMS/P.PRJ")).unwrap(), b"one");
        assert_eq!(fs::read(dir.join("mods/population-palette/merged/P.PRJ")).unwrap(), b"one");
        assert!(plan(&dir, Some((&f.payload, &mods)), Some(receipt.clone())).is_satisfied());

        let mods = vec![with(b"two")];
        assert!(!plan(&dir, Some((&f.payload, &mods)), Some(receipt.clone())).is_satisfied());
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &mods)), Some(receipt))]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("SYSTEMS/P.PRJ")).unwrap(), b"two");

        // No more changes: the file goes and the game uses its own again.
        let mods = vec![resource_mod(&f.root, "population-palette", &[])];
        run(&f, &[plan(&dir, Some((&f.payload, &mods)), Some(receipt))]).unwrap();
        assert!(!dir.join("SYSTEMS").exists());
        assert!(!dir.join("mods/population-palette/merged").exists());
    }

    #[test]
    fn places_resources_and_restores_loose_originals() {
        let f = fixture("deploy-assets");
        let dir = game(&f.root, "game");
        fs::create_dir_all(dir.join("CHARS/A")).unwrap();
        fs::write(dir.join("CHARS/A/B.GHG"), b"original").unwrap();
        fs::write(dir.join("CHARS/A/SAME.GHG"), b"same").unwrap();
        let before = snapshot(&dir);
        let mods = vec![resource_mod(&f.root, "r", &[("chars/a/b.ghg", b"new"), ("chars/new/c.tex", b"added"), ("CHARS/A/SAME.GHG", b"same")])];

        // A failure after the original was set aside puts everything back.
        let mut d = Deployment::begin(&f.journal).unwrap();
        d.fail_at_move = Some(5);
        assert!(d.apply(&plan(&dir, Some((&f.payload, &mods)), None)).is_err());
        d.rollback().unwrap();
        assert_eq!(snapshot(&dir), before);

        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &mods)), None)]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("CHARS/A/B.GHG")).unwrap(), b"new");
        assert_eq!(fs::read(dir.join("CHARS/NEW/C.TEX")).unwrap(), b"added");
        assert_eq!(fs::read(dir.join(ORIGINALS_DIR).join("CHARS/A/B.GHG")).unwrap(), b"original");
        assert_eq!(receipt.originals.len(), 1, "an identical loose file isn't kept as an original");
        assert_eq!(receipt.resources["CHARS/A/B.GHG"], "r");

        // Turning the mod off restores the original and removes the added file.
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &[])), Some(receipt))]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("CHARS/A/B.GHG")).unwrap(), b"original");
        assert!(!dir.join("CHARS/NEW").exists());
        assert!(!dir.join("CHARS/A/SAME.GHG").exists(), "the identical file belonged to the mod's earlier link");
        assert!(!dir.join(ORIGINALS_DIR).exists());
        assert!(receipt.originals.is_empty() && receipt.resources.is_empty());
    }

    #[test]
    fn a_chosen_mod_takes_over_shared_files() {
        let f = fixture("deploy-winner");
        let dir = game(&f.root, "game");
        let old = resource_mod(&f.root, "old", &[("chars/a/b.texture", b"old"), ("chars/a/c.texture", b"old-c")]);
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, std::slice::from_ref(&old))), None)]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("CHARS/A/B.TEXTURE")).unwrap(), b"old");

        // "new" is chosen over "old" for B: old gives it up and its installed manifest stops declaring it.
        let mut old = old;
        old.skipped = BTreeSet::from(["CHARS/A/B.TEXTURE".to_string()]);
        let new = resource_mod(&f.root, "new", &[("chars/a/b.texture", b"new")]);
        let mods = vec![new, old];
        assert!(!plan(&dir, Some((&f.payload, &mods)), Some(receipt.clone())).is_satisfied());
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &mods)), Some(receipt))]).unwrap().remove(0).unwrap();
        assert_eq!(fs::read(dir.join("CHARS/A/B.TEXTURE")).unwrap(), b"new");
        assert_eq!(fs::read(dir.join("CHARS/A/C.TEXTURE")).unwrap(), b"old-c");
        assert_eq!(receipt.resources["CHARS/A/B.TEXTURE"], "new");
        let installed = fs::read_to_string(dir.join("mods/old/mod.json")).unwrap();
        assert!(!installed.contains("CHARS/A/B.TEXTURE") && !installed.contains("chars/a/b.texture"), "{installed}");
        assert!(installed.contains("chars/a/c.texture"));
        assert!(plan(&dir, Some((&f.payload, &mods)), Some(receipt)).is_satisfied());
    }

    #[test]
    fn leaves_an_unknown_proxy_alone() {
        let f = fixture("deploy-unknown");
        let dir = game(&f.root, "game");
        fs::write(dir.join("dinput8.dll"), b"another tool").unwrap();
        let before = snapshot(&dir);
        let error = run(&f, &[plan(&dir, Some((&f.payload, &[])), None)]).unwrap_err();
        assert!(error.contains("doesn't belong to OMP"), "{error}");
        assert_eq!(snapshot(&dir), before);
    }

    #[test]
    fn a_failure_on_the_second_target_rolls_back_both() {
        let f = fixture("deploy-rollback");
        let (steam, epic) = (game(&f.root, "steam"), game(&f.root, "epic"));
        let old = vec![deploy_mod(&f.root, "a", "1.0.0", "x")];
        let receipts =
            run(&f, &[plan(&steam, Some((&f.payload, &old)), None), plan(&epic, Some((&f.payload, &old)), None)]).unwrap();
        let (before_steam, before_epic) = (snapshot(&steam), snapshot(&epic));

        let new_payload = payload(&f.root, "2", "driver-2");
        let new = vec![deploy_mod(&f.root, "a", "1.1.0", "y"), deploy_mod(&f.root, "b", "1.0.0", "x")];
        let mut d = Deployment::begin(&f.journal).unwrap();
        d.apply(&plan(&steam, Some((&new_payload, &new)), receipts[0].clone())).unwrap();
        let moves_so_far = d.moves.len();
        d.fail_at_move = Some(moves_so_far + 2);
        assert!(d.apply(&plan(&epic, Some((&new_payload, &new)), receipts[1].clone())).is_err());
        d.rollback().unwrap();

        assert_eq!(snapshot(&steam), before_steam);
        assert_eq!(snapshot(&epic), before_epic);
        assert!(!f.journal.exists());
    }

    #[test]
    fn recovers_an_interrupted_change_after_restart() {
        let f = fixture("deploy-recover");
        let dir = game(&f.root, "game");
        let before = snapshot(&dir);
        let mods = vec![deploy_mod(&f.root, "a", "1.0.0", "x")];
        let mut d = Deployment::begin(&f.journal).unwrap();
        d.fail_at_move = Some(2);
        assert!(d.apply(&plan(&dir, Some((&f.payload, &mods)), None)).is_err());
        drop(d); // The app "crashes" without rolling back.
        assert_ne!(snapshot(&dir), before);

        assert!(recover(&f.journal, |_| panic!("nothing was committed")).unwrap());
        assert_eq!(snapshot(&dir), before);
        assert!(!recover(&f.journal, |_| Ok(())).unwrap(), "nothing left to recover");
    }

    #[test]
    fn a_committed_change_finishes_after_restart() {
        let f = fixture("deploy-committed");
        let dir = game(&f.root, "game");
        let mut d = Deployment::begin(&f.journal).unwrap();
        let receipt = d.apply(&plan(&dir, Some((&f.payload, &[])), None)).unwrap();
        let receipts = BTreeMap::from([("game".to_string(), receipt)]);
        let mut saved = None;
        let result = d.commit(receipts.clone(), |_| Err("disk full".into()));
        assert!(result.is_err());
        recover(&f.journal, |r| {
            saved = Some(r.clone());
            Ok(())
        })
        .unwrap();
        assert_eq!(saved, Some(receipts));
        assert!(dir.join("dinput8.dll").is_file());
        assert!(!fs::read_dir(&dir).unwrap().flatten().any(|e| e.file_name().to_string_lossy().starts_with(TXN_PREFIX)));
    }

    #[test]
    fn refuses_to_overwrite_an_owned_file_someone_changed() {
        let f = fixture("deploy-changed");
        let dir = game(&f.root, "game");
        let receipt = run(&f, &[plan(&dir, Some((&f.payload, &[])), None)]).unwrap().remove(0);
        fs::write(dir.join("omp-lswtss-driver.dll"), b"edited").unwrap();
        let error = run(&f, &[plan(&dir, None, receipt.clone())]).unwrap_err();
        assert!(error.contains("changed outside"), "{error}");
        assert_eq!(fs::read(dir.join("omp-lswtss-driver.dll")).unwrap(), b"edited");

        // Choosing the Mod Manager's copy keeps the edited file aside and lets the update run.
        let receipt = receipt.unwrap();
        assert_eq!(set_aside_edited(&receipt).unwrap(), vec!["omp-lswtss-driver.dll".to_string()]);
        let kept: Vec<_> = fs::read_dir(dir.join(EDITED_DIR)).unwrap().flatten().collect();
        assert_eq!(fs::read(kept[0].path().join("omp-lswtss-driver.dll")).unwrap(), b"edited");
        run(&f, &[plan(&dir, Some((&f.payload, &[])), Some(receipt))]).unwrap();
        assert_ne!(fs::read(dir.join("omp-lswtss-driver.dll")).unwrap(), b"edited");
    }
}
