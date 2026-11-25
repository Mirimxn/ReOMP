//! Tauri commands for the Mods screen. The UI calls these; all filesystem
//! work and validation stays here.

use std::collections::BTreeSet;
use std::path::PathBuf;
use std::sync::{Mutex, MutexGuard};
use std::thread;

use serde::Serialize;
use tauri::{AppHandle, Emitter, Manager, State};
use tauri_plugin_dialog::DialogExt;

use crate::deployer::{self, DeployState, DeployStatus};
use crate::legacy;
use crate::library::{self, Library};
use crate::resolve::{self, Problem};
use crate::settings::{self, ManagerState};
use crate::targets::{self, TargetKind};
use crate::version::Version;
use crate::watch;

pub const LIBRARY_CHANGED: &str = "library-changed";

#[derive(Default)]
pub struct Core {
    library_dir: Option<PathBuf>,
    library_error: Option<String>,
    state_file: Option<PathBuf>,
    pub(crate) state: ManagerState,
    pub(crate) library: Library,
    watcher: Option<watch::Watcher>,
    watcher_error: Option<String>,
    pub(crate) deploy: DeployState,
    /// IDs of the core mods bundled with this build.
    bundled_ids: BTreeSet<String>,
}

#[derive(Default)]
pub struct AppState(pub Mutex<Core>);

impl Core {
    /// Returns true while a ZIP still looks like it is being copied.
    fn rescan(&mut self) -> bool {
        let Some(dir) = self.library_dir.clone() else { return false };
        match library::scan(&dir) {
            Ok(found) => {
                let copying = found.copying;
                self.library = found;
                self.library_error = None;
                copying
            }
            Err(e) => {
                self.library_error = Some(format!("Can't read the mod library folder {}: {e}", dir.display()));
                false
            }
        }
    }

    /// Deletes one version's ZIPs from the library unless the current
    /// selection still needs that version.
    fn remove_version(&mut self, id: &str, version: Version) -> Outcome {
        let Some(entry) = self.library.version(id, version).cloned() else {
            return Outcome::rejected("That version is no longer in the library.", None::<String>);
        };
        let mut proposed = self.library.clone();
        if let Some(versions) = proposed.packages.get_mut(id) {
            versions.remove(&version);
            if versions.is_empty() {
                proposed.packages.remove(id);
            }
        }
        let current = self.selection_for(&self.state).problems;
        let after = resolve::resolve_with(&proposed, &self.state.enabled, &self.state.pinned, &self.state.winners, &self.state.file_choices).problems;
        let added: Vec<String> = after.into_iter().filter(|p| !current.contains(p)).map(|p| p.message).collect();
        if !added.is_empty() {
            let mut details = added;
            details.push(format!("Disable {id} or pick another version first."));
            return Outcome::rejected(&format!("{id} {version} is still needed, so it wasn't removed:"), details);
        }
        let mut failed = Vec::new();
        for file in entry.files.keys() {
            if let Err(e) = std::fs::remove_file(file) {
                failed.push(format!("{}: {e}", file.display()));
            }
        }
        self.rescan();
        self.deploy.kick();
        if failed.is_empty() {
            Outcome::ok(vec![format!("Removed {id} {version}")])
        } else {
            Outcome::rejected("Some files couldn't be removed:", failed)
        }
    }

    /// Every game folder the manager knows, chosen or installed into.
    fn game_dirs(&self) -> Vec<PathBuf> {
        let mut dirs: Vec<PathBuf> = self.state.targets.values().filter_map(|t| t.path.clone()).collect();
        for receipt in self.deploy.receipts.values() {
            if !dirs.iter().any(|d| targets::same_dir(d, &receipt.game_dir)) {
                dirs.push(receipt.game_dir.clone());
            }
        }
        dirs
    }

    /// `id` and every mod it needs, transitively, that the library has.
    fn with_dependencies(&self, id: &str) -> BTreeSet<String> {
        let mut found = BTreeSet::new();
        let mut queue = vec![id.to_string()];
        while let Some(next) = queue.pop() {
            if !found.insert(next.clone()) {
                continue;
            }
            let Some(versions) = self.library.packages.get(&next) else { continue };
            let entry = self.state.pinned.get(&next).and_then(|v| versions.get(v)).or_else(|| versions.values().next_back());
            if let Some(entry) = entry {
                queue.extend(entry.info.manifest.dependencies.iter().map(|d| d.id.clone()));
            }
        }
        found
    }

    fn selection_for(&self, state: &ManagerState) -> resolve::Selection {
        resolve::resolve_with(&self.library, &state.enabled, &state.pinned, &state.winners, &state.file_choices)
    }

    /// Applies a change to the saved selection only if it adds no new problems.
    fn change(&mut self, edit: impl FnOnce(&mut ManagerState)) -> Outcome {
        let mut proposed = self.state.clone();
        edit(&mut proposed);
        let current = self.selection_for(&self.state).problems;
        let added: Vec<Problem> = self
            .selection_for(&proposed)
            .problems
            .into_iter()
            .filter(|p| !current.contains(p))
            .collect();
        if !added.is_empty() {
            let mut choices: Vec<Choice> = added
                .iter()
                .filter_map(|p| p.pair.clone().map(|(a, b)| Choice { a, b, paths: p.paths.clone() }))
                .collect();
            choices.sort_by(|x, y| (&x.a, &x.b).cmp(&(&y.a, &y.b)));
            choices.dedup_by(|x, y| x.a == y.a && x.b == y.b);
            let mut outcome = Outcome::rejected("That change was blocked, so nothing changed:", added.iter().map(|p| p.message.clone()));
            outcome.details.sort();
            outcome.details.dedup();
            outcome.choices = choices;
            return outcome;
        }
        let Some(file) = self.state_file.clone() else {
            return Outcome::rejected("The manager's settings aren't available.", None::<String>);
        };
        if let Err(e) = settings::save_state(&file, &proposed) {
            return Outcome::rejected("Couldn't save the change:", Some(e));
        }
        self.state = proposed;
        self.deploy.kick();
        Outcome::ok(Vec::new())
    }
}

/// Two mods (sorted) and the game paths both place that still need a choice.
#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Choice {
    a: String,
    b: String,
    paths: Vec<String>,
}

/// Files this mod and `other` both place, with whose copy each uses.
#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ShareView {
    other: String,
    files: Vec<SharedFile>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SharedFile {
    path: String,
    /// The mod whose copy is used, or none while the clash is undecided.
    used: Option<String>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Outcome {
    ok: bool,
    title: String,
    details: Vec<String>,
    /// A mod that can be enabled together with the mods it needs.
    #[serde(skip_serializing_if = "Option::is_none")]
    offer: Option<String>,
    /// Pairs of mods placing the same files; the player picks whose files to use.
    #[serde(skip_serializing_if = "Vec::is_empty")]
    choices: Vec<Choice>,
    /// The mod being enabled when the change was blocked, to retry after a choice.
    #[serde(skip_serializing_if = "Option::is_none")]
    retry_enable: Option<String>,
    /// Files that only lack the newer manifest fields and can be imported with them added.
    #[serde(skip_serializing_if = "Vec::is_empty")]
    auto_version: Vec<String>,
}

impl Outcome {
    fn ok(details: Vec<String>) -> Self {
        Outcome { ok: true, title: String::new(), details, offer: None, choices: Vec::new(), retry_enable: None, auto_version: Vec::new() }
    }
    fn rejected(title: &str, details: impl IntoIterator<Item = String>) -> Self {
        Outcome {
            ok: false,
            title: title.to_string(),
            details: details.into_iter().collect(),
            offer: None,
            choices: Vec::new(),
            retry_enable: None,
            auto_version: Vec::new(),
        }
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct VersionView {
    version: Version,
    files: Vec<String>,
    conflict: bool,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ModView {
    id: String,
    name: String,
    description: Option<String>,
    /// Newest first.
    versions: Vec<VersionView>,
    enabled: bool,
    pinned: Option<Version>,
    selected: Option<Version>,
    /// Version installed in the game by the last deployment, when any.
    deployed: Option<Version>,
    /// Mods whose shared files this mod's files were chosen over.
    chosen_over: Vec<String>,
    /// Other enabled mods placing some of the same files.
    shares: Vec<ShareView>,
    problems: Vec<String>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct InvalidView {
    file: String,
    reason: String,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct TargetView {
    kind: TargetKind,
    label: &'static str,
    path: Option<String>,
    enabled: bool,
    installed: bool,
    /// Mods installed by the last successful deployment, `id version`.
    installed_mods: Vec<String>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LegacyView {
    id: String,
    name: String,
    folder: String,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LibraryView {
    targets: Vec<TargetView>,
    /// Old mods in legacy-mods that aren't in the library and aren't bundled.
    legacy: Vec<LegacyView>,
    deploy: DeployStatus,
    omp_version: Option<String>,
    library_dir: Option<String>,
    library_error: Option<String>,
    watcher_error: Option<String>,
    mods: Vec<ModView>,
    invalid: Vec<InvalidView>,
}

fn view(core: &Core) -> LibraryView {
    let selection = core.selection_for(&core.state);
    let ids: BTreeSet<&String> = core.library.packages.keys().chain(core.state.enabled.iter()).collect();
    let mods = ids
        .into_iter()
        .map(|id| {
            let versions = core.library.packages.get(id);
            let newest = versions.and_then(|v| v.values().next_back());
            ModView {
                id: id.clone(),
                name: newest.map_or_else(|| id.clone(), |e| e.info.manifest.name.clone()),
                description: newest.and_then(|e| e.info.manifest.description.clone()),
                versions: versions
                    .map(|v| {
                        v.iter()
                            .rev()
                            .map(|(version, entry)| VersionView {
                                version: *version,
                                files: entry.files.keys().map(|p| p.display().to_string()).collect(),
                                conflict: entry.is_conflict(),
                            })
                            .collect()
                    })
                    .unwrap_or_default(),
                enabled: core.state.enabled.contains(id),
                pinned: core.state.pinned.get(id).copied(),
                selected: selection.selected.get(id).copied(),
                chosen_over: core
                    .state
                    .winners
                    .iter()
                    .filter(|(_, winner)| *winner == id)
                    .filter_map(|(key, _)| key.split('|').find(|other| *other != id.as_str()).map(str::to_string))
                    .collect(),
                deployed: core.deploy.receipts.values().find_map(|r| r.mods.get(id.as_str()).map(|m| m.version)),
                shares: selection
                    .shared
                    .iter()
                    .filter_map(|((a, b), paths)| {
                        let other = if a == id { b } else if b == id { a } else { return None };
                        let gives_up = |m: &String, p: &String| selection.skipped.get(m).is_some_and(|s| s.contains(p));
                        let files = paths
                            .iter()
                            .map(|path| SharedFile {
                                path: path.clone(),
                                used: match (gives_up(id, path), gives_up(other, path)) {
                                    (false, true) => Some(id.clone()),
                                    (true, false) => Some(other.clone()),
                                    _ => None,
                                },
                            })
                            .collect();
                        Some(ShareView { other: other.clone(), files })
                    })
                    .collect(),
                problems: selection.problems.iter().filter(|p| &p.mod_id == id).map(|p| p.message.clone()).collect(),
            }
        })
        .collect();
    let targets = TargetKind::ALL
        .iter()
        .map(|&kind| {
            let settings = core.state.targets.get(&kind).cloned().unwrap_or_default();
            let receipt = core.deploy.receipts.get(kind.key());
            TargetView {
                kind,
                label: kind.label(),
                path: settings.path.map(|p| p.display().to_string()),
                enabled: settings.enabled,
                installed: receipt.is_some(),
                installed_mods: receipt
                    .map(|r| r.mods.iter().map(|(id, m)| format!("{id} {}", m.version)).collect())
                    .unwrap_or_default(),
            }
        })
        .collect();
    let legacy = legacy::find(&core.game_dirs())
        .into_iter()
        .filter(|m| !core.library.packages.contains_key(&m.id) && !core.bundled_ids.contains(&m.id))
        .map(|m| LegacyView { id: m.id, name: m.name, folder: m.folder.display().to_string() })
        .collect();
    LibraryView {
        targets,
        legacy,
        deploy: core.deploy.status.clone(),
        omp_version: match &core.deploy.payload {
            Some(Ok(p)) => Some(p.version.clone()),
            _ => None,
        },
        library_dir: core.library_dir.as_ref().map(|p| p.display().to_string()),
        library_error: core.library_error.clone(),
        watcher_error: core.watcher_error.clone(),
        mods,
        invalid: core
            .library
            .invalid
            .iter()
            .map(|i| InvalidView { file: i.file.display().to_string(), reason: i.reason.clone() })
            .collect(),
    }
}

pub(crate) fn lock(state: &AppState) -> MutexGuard<'_, Core> {
    state.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner())
}

pub(crate) fn notify(app: &AppHandle) {
    let _ = app.emit(LIBRARY_CHANGED, ());
}

fn start_watcher(app: &AppHandle, core: &mut Core) {
    core.watcher = None;
    let Some(dir) = core.library_dir.clone() else { return };
    match watch::start(app.clone(), dir) {
        Ok(watcher) => {
            core.watcher = Some(watcher);
            core.watcher_error = None;
        }
        Err(e) => core.watcher_error = Some(e),
    }
}

/// Loads settings, starts the watcher before the first scan, then scans.
pub fn init(app: &AppHandle) {
    let paths = app.path();
    let dirs = paths.app_config_dir().and_then(|c| paths.app_data_dir().map(|d| (c, d)));
    let copying = {
        let state = app.state::<AppState>();
        let mut core = lock(&state);
        match dirs {
            Err(e) => core.library_error = Some(format!("Couldn't find the app's settings folders: {e}")),
            Ok((config_dir, data_dir)) => match settings::load(&config_dir, &data_dir) {
                Ok(loaded) => {
                    core.library_dir = Some(loaded.library_dir);
                    core.deploy.data_dir = Some(loaded.data_dir);
                    core.state_file = Some(loaded.state_file.clone());
                    core.state = loaded.state;
                    // Suggest game folders the player hasn't chosen yet.
                    let mut found = false;
                    for kind in TargetKind::ALL {
                        let entry = core.state.targets.entry(kind).or_default();
                        if entry.path.is_none() {
                            entry.path = targets::discover(kind);
                            found |= entry.path.is_some();
                        }
                    }
                    if found {
                        let _ = settings::save_state(&loaded.state_file, &core.state);
                    }
                }
                Err(e) => core.library_error = Some(e),
            },
        }
        start_watcher(app, &mut core);
        let copying = core.rescan();
        import_bundled(app, &mut core);
        deployer::init(app, &mut core);
        copying
    };
    if copying {
        let app = app.clone();
        thread::spawn(move || watch::refresh_until_settled(&app));
    }
}

/// Adds mod ZIPs shipped with this build that the library hasn't been
/// offered yet, through the normal import checks. Core mods are only added;
/// default mods are also enabled, with the mods they need, when first added.
fn import_bundled(app: &AppHandle, core: &mut Core) {
    let Ok(resources) = app.path().resource_dir() else { return };
    import_bundled_from(core, &resources.join("core-mods"), &resources.join("default-mods"));
}

fn import_bundled_from(core: &mut Core, core_dir: &std::path::Path, default_dir: &std::path::Path) {
    let Some(library_dir) = core.library_dir.clone() else { return };
    let mut offered = false;
    let mut to_enable = Vec::new();
    for (dir, enable) in [(core_dir, false), (default_dir, true)] {
        let Ok(entries) = std::fs::read_dir(dir) else { continue };
        let mut paths: Vec<PathBuf> = entries.flatten().map(|e| e.path()).collect();
        paths.sort();
        for path in paths {
            let Ok(info) = crate::package::read_package(&path) else { continue };
            core.bundled_ids.insert(info.id.clone());
            let key = format!("{}@{}", info.id, info.manifest.version);
            if core.state.bundled_imported.contains(&key) {
                continue;
            }
            if library::import(&library_dir, &core.library, &[path]).is_ok() {
                core.state.bundled_imported.insert(key);
                offered = true;
                core.rescan();
                if enable {
                    to_enable.push(info.id);
                }
            }
        }
    }
    if offered {
        if let Some(file) = &core.state_file {
            let _ = settings::save_state(file, &core.state);
        }
    }
    for id in to_enable {
        let needed = core.with_dependencies(&id);
        // Skipped, not forced, if it would break the current selection.
        let _ = core.change(|s| s.enabled.extend(needed));
    }
    // An updated core mod can need a core mod that is new in this release
    // (v1 0.2.0 needs asset-replacement). Turn such bundled mods on, on every
    // start, so an earlier update that missed it is fixed too.
    let missing: BTreeSet<String> = core
        .state
        .enabled
        .iter()
        .flat_map(|id| core.with_dependencies(id))
        .filter(|id| !core.state.enabled.contains(id) && core.bundled_ids.contains(id) && core.library.packages.contains_key(id))
        .collect();
    if !missing.is_empty() {
        let _ = core.change(|s| s.enabled.extend(missing));
    }
}

/// Called by the watcher thread. Returns true if another check is needed soon.
pub fn rescan_and_notify(app: &AppHandle) -> bool {
    let copying = {
        let state = app.state::<AppState>();
        let mut core = lock(&state);
        let copying = core.rescan();
        core.deploy.kick();
        copying
    };
    notify(app);
    copying
}

pub fn report_watcher_failure(app: &AppHandle, error: String) {
    {
        let state = app.state::<AppState>();
        let mut core = lock(&state);
        core.watcher_error = Some(format!("The mod library folder stopped being watched: {error}"));
    }
    notify(app);
}

fn import_paths(app: &AppHandle, paths: Vec<PathBuf>) -> Outcome {
    if paths.is_empty() {
        return Outcome::ok(Vec::new());
    }
    let state = app.state::<AppState>();
    let mut core = lock(&state);
    let Some(dir) = core.library_dir.clone() else {
        return Outcome::rejected("The mod library folder isn't available.", core.library_error.clone());
    };
    // Scan first so the batch is checked against what is on disk right now.
    core.rescan();
    let outcome = match library::import(&dir, &core.library, &paths) {
        Ok(summary) => Outcome::ok(
            summary
                .added
                .into_iter()
                .map(|a| format!("Added {a}"))
                .chain(summary.skipped)
                .collect(),
        ),
        Err(errors) => {
            let mut outcome = Outcome::rejected("Nothing was imported:", errors);
            let temp_dir = core.deploy.data_dir.as_ref().map(|d| d.join("tmp"));
            if let Some(temp_dir) = temp_dir {
                outcome.auto_version = paths
                    .iter()
                    .filter(|p| crate::package::read_package(p).is_err() && legacy::can_upgrade_zip(p, &temp_dir))
                    .map(|p| p.display().to_string())
                    .collect();
            }
            outcome
        }
    };
    core.rescan();
    core.deploy.kick();
    drop(core);
    notify(app);
    outcome
}

#[tauri::command]
pub async fn get_library(state: State<'_, AppState>) -> Result<LibraryView, String> {
    Ok(view(&lock(&state)))
}

/// Shared by drag-and-drop and the file picker.
#[tauri::command]
pub async fn import_zips(app: AppHandle, paths: Vec<String>) -> Result<Outcome, String> {
    let paths = paths.into_iter().map(PathBuf::from).collect();
    tauri::async_runtime::spawn_blocking(move || import_paths(&app, paths))
        .await
        .map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn pick_and_import(app: AppHandle) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let picked = app.dialog().file().add_filter("Mod ZIP", &["zip"]).blocking_pick_files();
        let paths: Vec<PathBuf> = picked
            .unwrap_or_default()
            .into_iter()
            .filter_map(|p| p.into_path().ok())
            .collect();
        import_paths(&app, paths)
    })
    .await
    .map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn set_enabled(
    app: AppHandle,
    state: State<'_, AppState>,
    id: String,
    enabled: bool,
    with_dependencies: Option<bool>,
) -> Result<Outcome, String> {
    let outcome = {
        let mut core = lock(&state);
        let needed = core.with_dependencies(&id);
        if enabled && with_dependencies == Some(true) {
            core.change(|s| s.enabled.extend(needed))
        } else {
            let mut outcome = core.change(|s| {
                if enabled {
                    s.enabled.insert(id.clone());
                } else {
                    s.enabled.remove(&id);
                }
            });
            let missing = needed.iter().any(|d| !core.state.enabled.contains(d) && *d != id);
            if enabled && !outcome.ok && missing {
                outcome.offer = Some(id.clone());
            }
            if enabled && !outcome.ok && !outcome.choices.is_empty() {
                outcome.retry_enable = Some(id.clone());
            }
            outcome
        }
    };
    notify(&app);
    Ok(outcome)
}

#[tauri::command]
pub async fn set_pinned_version(
    app: AppHandle,
    state: State<'_, AppState>,
    id: String,
    version: Option<String>,
) -> Result<Outcome, String> {
    let version = match version.as_deref().map(Version::parse) {
        Some(Err(e)) => return Ok(Outcome::rejected("That version isn't valid:", Some(e))),
        Some(Ok(v)) => Some(v),
        None => None,
    };
    let outcome = lock(&state).change(|s| match version {
        Some(v) => {
            s.pinned.insert(id.clone(), v);
        }
        None => {
            s.pinned.remove(&id);
        }
    });
    notify(&app);
    Ok(outcome)
}

#[tauri::command]
pub async fn retry_watcher(app: AppHandle, state: State<'_, AppState>) -> Result<(), String> {
    {
        let mut core = lock(&state);
        start_watcher(&app, &mut core);
        core.rescan();
    }
    notify(&app);
    Ok(())
}

#[tauri::command]
pub async fn pick_game_folder(app: AppHandle, kind: TargetKind) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let Some(picked) = app.dialog().file().set_title(format!("Choose the {} game folder", kind.label())).blocking_pick_folder() else {
            return Outcome::ok(Vec::new());
        };
        let path = match picked.into_path() {
            Ok(path) => path,
            Err(e) => return Outcome::rejected("Can't use that folder:", Some(e.to_string())),
        };
        let dir = match targets::validate_game_dir(&path) {
            Ok(dir) => dir,
            Err(e) => return Outcome::rejected("That isn't the game folder:", Some(e)),
        };
        let state = app.state::<AppState>();
        let mut core = lock(&state);
        let clash = TargetKind::ALL.into_iter().find(|&other| {
            other != kind
                && core.state.targets.get(&other).and_then(|t| t.path.as_ref()).is_some_and(|p| targets::same_dir(p, &dir))
        });
        if let Some(other) = clash {
            return Outcome::rejected(
                "That folder is already used:",
                Some(format!("{} is set as the {} game folder.", dir.display(), other.label())),
            );
        }
        let outcome = core.change(|s| s.targets.entry(kind).or_default().path = Some(dir));
        drop(core);
        notify(&app);
        outcome
    })
    .await
    .map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn set_target_enabled(
    app: AppHandle,
    state: State<'_, AppState>,
    kind: TargetKind,
    enabled: bool,
) -> Result<Outcome, String> {
    let outcome = {
        let mut core = lock(&state);
        let path = core.state.targets.get(&kind).and_then(|t| t.path.clone());
        match (enabled, path) {
            (true, None) => Outcome::rejected("Choose the game folder first.", None::<String>),
            (true, Some(path)) if targets::validate_game_dir(&path).is_err() => Outcome::rejected(
                "That game folder isn't usable:",
                targets::validate_game_dir(&path).err(),
            ),
            _ => core.change(|s| s.targets.entry(kind).or_default().enabled = enabled),
        }
    };
    notify(&app);
    Ok(outcome)
}

#[tauri::command]
pub async fn retry_deploy(app: AppHandle, state: State<'_, AppState>) -> Result<(), String> {
    {
        let mut core = lock(&state);
        if core.deploy.recovery_error.is_some() {
            deployer::recover(&mut core.deploy);
        }
        core.deploy.request_retry();
    }
    notify(&app);
    Ok(())
}

/// Packs an old mod from legacy-mods into the library.
#[tauri::command]
pub async fn add_legacy_mod(app: AppHandle, folder: String) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let (found, temp_dir) = {
            let state = app.state::<AppState>();
            let core = lock(&state);
            let found = legacy::find(&core.game_dirs()).into_iter().find(|m| m.folder.display().to_string() == folder);
            (found, core.deploy.data_dir.as_ref().map(|d| d.join("tmp")))
        };
        let (Some(found), Some(temp_dir)) = (found, temp_dir) else {
            return Outcome::rejected("That old mod is no longer there.", None::<String>);
        };
        if let Err(e) = std::fs::create_dir_all(&temp_dir) {
            return Outcome::rejected("Couldn't prepare the mod:", Some(e.to_string()));
        }
        let zip = temp_dir.join(format!("{}-{}.zip", found.id, legacy::LEGACY_VERSION));
        let outcome = match legacy::pack(&found.folder, &found.id, &zip) {
            Ok(()) => import_paths(&app, vec![zip.clone()]),
            Err(e) => Outcome::rejected("Couldn't pack that mod:", Some(e)),
        };
        let _ = std::fs::remove_file(&zip);
        outcome
    })
    .await
    .map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn remove_version(app: AppHandle, state: State<'_, AppState>, id: String, version: String) -> Result<Outcome, String> {
    let outcome = match Version::parse(&version) {
        Ok(version) => lock(&state).remove_version(&id, version),
        Err(e) => Outcome::rejected("That version isn't valid:", Some(e)),
    };
    notify(&app);
    Ok(outcome)
}

/// The game file index, kept apart from `Core` so building it never blocks the rest of the app.
#[derive(Default)]
pub struct AssetState(pub Mutex<Option<std::sync::Arc<crate::assets::AssetIndex>>>);

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetSummary {
    game_dir: String,
    archives: Vec<(String, usize)>,
    archive_errors: Vec<String>,
    loose: usize,
    total: usize,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetListing {
    folders: Vec<String>,
    files: Vec<crate::assets::ItemView>,
    more: bool,
}

fn asset_index(app: &AppHandle) -> Result<std::sync::Arc<crate::assets::AssetIndex>, String> {
    app.state::<AssetState>()
        .0
        .lock()
        .unwrap_or_else(|p| p.into_inner())
        .clone()
        .ok_or_else(|| "Open a game folder first.".to_string())
}

/// Builds (or reuses) the index for a target's game folder.
#[tauri::command]
pub async fn assets_open(app: AppHandle, kind: TargetKind) -> Result<AssetSummary, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let path = {
            let state = app.state::<AppState>();
            let core = lock(&state);
            core.state.targets.get(&kind).and_then(|t| t.path.clone())
        };
        let path = path.ok_or_else(|| format!("Choose the {} game folder in the Mods tab first.", kind.label()))?;
        let game_dir = targets::validate_game_dir(&path)?;
        let cached = app.state::<AssetState>().0.lock().unwrap_or_else(|p| p.into_inner()).clone();
        let index = match cached {
            Some(index) if index.is_current(&game_dir) => index,
            _ => {
                let built = std::sync::Arc::new(crate::assets::AssetIndex::build(&game_dir));
                *app.state::<AssetState>().0.lock().unwrap_or_else(|p| p.into_inner()) = Some(built.clone());
                built
            }
        };
        let loose = index.items.iter().filter(|i| i.source == crate::assets::LOOSE).count();
        Ok(AssetSummary {
            game_dir: index.game_dir.display().to_string(),
            archives: index
                .archives
                .iter()
                .map(|a| (a.file.file_name().unwrap_or_default().to_string_lossy().into_owned(), a.entries.len()))
                .collect(),
            archive_errors: index.archive_errors.clone(),
            loose,
            total: index.items.len(),
        })
    })
    .await
    .map_err(|e| e.to_string())?
}

#[tauri::command]
pub async fn assets_list(app: AppHandle, folder: String) -> Result<AssetListing, String> {
    let index = asset_index(&app)?;
    let (folders, files, more) = index.list(&folder, 2000);
    Ok(AssetListing { folders, files: files.into_iter().map(|i| crate::assets::ItemView::of(&index, i)).collect(), more })
}

#[tauri::command]
pub async fn assets_search(app: AppHandle, query: String, folder: String) -> Result<AssetListing, String> {
    let index = asset_index(&app)?;
    let (files, more) = index.search(&query, &folder, 1000);
    Ok(AssetListing { folders: Vec::new(), files: files.into_iter().map(|i| crate::assets::ItemView::of(&index, i)).collect(), more })
}

/// Every file in the game index for the character maker, as text: the first line
/// lists the archive names (tab-separated), then one `<source>\t<path>` line per
/// file, where `<source>` is the archive's position or `L` for a loose file.
#[tauri::command]
pub async fn assets_paths(app: AppHandle) -> Result<tauri::ipc::Response, String> {
    let index = asset_index(&app)?;
    tauri::async_runtime::spawn_blocking(move || {
        let mut out = String::with_capacity(index.items.len() * 64);
        let names: Vec<String> = index
            .archives
            .iter()
            .map(|a| a.file.file_name().unwrap_or_default().to_string_lossy().into_owned())
            .collect();
        out.push_str(&names.join("\t"));
        out.push('\n');
        for item in &index.items {
            if item.source == crate::assets::LOOSE {
                out.push('L');
            } else {
                out.push_str(&item.source.to_string());
            }
            out.push('\t');
            out.push_str(&item.path);
            out.push('\n');
        }
        tauri::ipc::Response::new(out.into_bytes())
    })
    .await
    .map_err(|e| e.to_string())
}

/// One game file's bytes, for the character maker.
#[tauri::command]
pub async fn assets_read(app: AppHandle, path: String, source: u32) -> Result<tauri::ipc::Response, String> {
    let index = asset_index(&app)?;
    tauri::async_runtime::spawn_blocking(move || {
        let item = index.find(&path, source).ok_or_else(|| format!("{path} isn't in the game index"))?;
        let codecs = crate::assets::GameCodecs::for_game(&index.game_dir);
        index.read(item, &codecs).map(tauri::ipc::Response::new)
    })
    .await
    .map_err(|e| e.to_string())?
}

/// A file name for a mod ZIP made in the app: letters, digits, `-`, `_` and `.`,
/// ending in `.zip`.
fn package_file_name(name: &str) -> String {
    let stem = name.trim().trim_end_matches(".zip").trim_end_matches(".ZIP");
    let clean: String = stem
        .chars()
        .map(|c| if c.is_ascii_alphanumeric() || matches!(c, '-' | '_' | '.') { c } else { '-' })
        .collect();
    let clean = clean.trim_matches(|c| c == '.' || c == '-').to_string();
    format!("{}.zip", if clean.is_empty() { "mod".to_string() } else { clean })
}

/// A mod ZIP made by the character maker (the request body, file name in the
/// `file-name` header). With `to-library: 1` it goes through the same import as
/// a dropped ZIP; otherwise the player picks where to save it.
#[tauri::command]
pub async fn package_bytes(app: AppHandle, request: tauri::ipc::Request<'_>) -> Result<Outcome, String> {
    let tauri::ipc::InvokeBody::Raw(bytes) = request.body() else {
        return Err("expected the ZIP as the request body".into());
    };
    let bytes = bytes.clone();
    let header = |name: &str| request.headers().get(name).and_then(|v| v.to_str().ok()).map(str::to_string);
    let file_name = package_file_name(&header("file-name").unwrap_or_default());
    let to_library = header("to-library").as_deref() == Some("1");
    tauri::async_runtime::spawn_blocking(move || {
        if to_library {
            let temp_dir = {
                let state = app.state::<AppState>();
                let core = lock(&state);
                core.deploy.data_dir.as_ref().map(|d| d.join("tmp"))
            };
            let Some(temp_dir) = temp_dir else {
                return Outcome::rejected("The mod library isn't available.", None::<String>);
            };
            let zip = temp_dir.join(&file_name);
            if let Err(e) = std::fs::create_dir_all(&temp_dir).and_then(|_| std::fs::write(&zip, &bytes)) {
                return Outcome::rejected("Couldn't prepare the mod:", Some(e.to_string()));
            }
            let outcome = import_paths(&app, vec![zip.clone()]);
            let _ = std::fs::remove_file(&zip);
            outcome
        } else {
            let Some(picked) = app
                .dialog()
                .file()
                .set_title("Save the mod ZIP")
                .set_file_name(&file_name)
                .add_filter("Mod ZIP", &["zip"])
                .blocking_save_file()
            else {
                return Outcome::ok(Vec::new());
            };
            match picked.into_path().map_err(|e| e.to_string()).and_then(|out| std::fs::write(&out, &bytes).map(|_| out).map_err(|e| e.to_string())) {
                Ok(out) => Outcome::ok(vec![format!("Saved {}.", out.display())]),
                Err(e) => Outcome::rejected("Couldn't save the mod:", Some(e)),
            }
        }
    })
    .await
    .map_err(|e| e.to_string())
}

#[derive(serde::Deserialize)]
pub struct AssetRef {
    path: String,
    source: u32,
}

/// Extracts the chosen files to a folder the player picks, keeping their game paths.
#[tauri::command]
pub async fn assets_extract(app: AppHandle, items: Vec<AssetRef>) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let index = match asset_index(&app) {
            Ok(index) => index,
            Err(e) => return Outcome::rejected(&e, None::<String>),
        };
        let Some(picked) = app.dialog().file().set_title("Choose where to extract the files").blocking_pick_folder() else {
            return Outcome::ok(Vec::new());
        };
        let dest = match picked.into_path() {
            Ok(dest) => dest,
            Err(e) => return Outcome::rejected("Can't use that folder:", Some(e.to_string())),
        };
        if targets::same_dir(&dest, &index.game_dir) || dest.starts_with(&index.game_dir) {
            return Outcome::rejected("Choose a folder outside the game folder, so the game's files stay untouched.", None::<String>);
        }
        let codecs = crate::assets::GameCodecs::for_game(&index.game_dir);
        let (mut done, mut failed) = (0usize, Vec::new());
        for wanted in &items {
            match index.find(&wanted.path, wanted.source) {
                Some(item) => match index.extract(item, &dest, &codecs) {
                    Ok(_) => done += 1,
                    Err(e) => failed.push(format!("{} ({}): {e}", item.path, index.source_name(item.source))),
                },
                None => failed.push(format!("{}: no longer in the game files", wanted.path)),
            }
        }
        if failed.is_empty() {
            Outcome::ok(vec![format!("Extracted {done} file{} to {}", if done == 1 { "" } else { "s" }, dest.display())])
        } else {
            let mut details = failed;
            if done > 0 {
                details.insert(0, format!("{done} other file{} extracted to {}", if done == 1 { " was" } else { "s were" }, dest.display()));
            }
            Outcome::rejected("Some files couldn't be extracted:", details)
        }
    })
    .await
    .map_err(|e| e.to_string())
}

/// Picks the player's replacement file for one game path.
#[tauri::command]
pub async fn draft_pick_file(app: AppHandle, game_path: String) -> Result<Option<String>, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let name = game_path.rsplit('/').next().unwrap_or("file").to_string();
        app.dialog()
            .file()
            .set_title(format!("Choose the replacement for {name}"))
            .blocking_pick_file()
            .and_then(|p| p.into_path().ok())
            .map(|p| p.display().to_string())
    })
    .await
    .map_err(|e| e.to_string())
}

/// Turns the draft into a mod ZIP: saved where the player chooses, or added to the library.
#[tauri::command]
pub async fn draft_build(app: AppHandle, draft: crate::draft::Draft, to_library: bool) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let index = match asset_index(&app) {
            Ok(index) => index,
            Err(e) => return Outcome::rejected(&e, None::<String>),
        };
        let mut problems = crate::draft::check(&draft, &index);
        let codecs = crate::assets::GameCodecs::for_game(&index.game_dir);
        let (fixed, notes, texture_problems) = crate::draft::prepare_textures(&draft, &index, &codecs);
        problems.extend(texture_problems);
        if !problems.is_empty() {
            return Outcome::rejected("The mod isn't ready yet:", problems);
        }
        let with_notes = |mut outcome: Outcome| {
            if outcome.ok && !notes.is_empty() {
                outcome.details.extend(notes.iter().cloned());
            }
            outcome
        };
        let file_name = format!("{}-{}.zip", crate::draft::mod_id(&draft.name), draft.version.trim());
        if to_library {
            let temp_dir = {
                let state = app.state::<AppState>();
                let core = lock(&state);
                core.deploy.data_dir.as_ref().map(|d| d.join("tmp"))
            };
            let Some(temp_dir) = temp_dir else {
                return Outcome::rejected("The mod library isn't available.", None::<String>);
            };
            if let Err(e) = std::fs::create_dir_all(&temp_dir) {
                return Outcome::rejected("Couldn't prepare the mod:", Some(e.to_string()));
            }
            let zip = temp_dir.join(&file_name);
            let outcome = match crate::draft::export_with(&draft, &fixed, &zip) {
                Ok(_) => with_notes(import_paths(&app, vec![zip.clone()])),
                Err(e) => Outcome::rejected("Couldn't build the mod:", Some(e)),
            };
            let _ = std::fs::remove_file(&zip);
            outcome
        } else {
            let Some(picked) = app
                .dialog()
                .file()
                .set_title("Save the mod ZIP")
                .set_file_name(&file_name)
                .add_filter("Mod ZIP", &["zip"])
                .blocking_save_file()
            else {
                return Outcome::ok(Vec::new());
            };
            let out = match picked.into_path() {
                Ok(out) => out,
                Err(e) => return Outcome::rejected("Can't save there:", Some(e.to_string())),
            };
            match crate::draft::export_with(&draft, &fixed, &out) {
                Ok(_) => with_notes(Outcome::ok(vec![format!("Saved {}", out.display())])),
                Err(e) => Outcome::rejected("Couldn't build the mod:", Some(e)),
            }
        }
    })
    .await
    .map_err(|e| e.to_string())
}

/// Uses all of `winner`'s files where it and `loser` place the same ones.
/// File-by-file choices for `paths` (the pair's shared files) are dropped so
/// the one-click choice covers every file.
#[tauri::command]
pub async fn set_file_winner(
    app: AppHandle,
    state: State<'_, AppState>,
    winner: String,
    loser: String,
    paths: Option<Vec<String>>,
) -> Result<Outcome, String> {
    let outcome = lock(&state).change(|s| {
        s.winners.insert(crate::settings::pair_key(&winner, &loser), winner.clone());
        for path in paths.iter().flatten() {
            s.file_choices.remove(path);
        }
    });
    notify(&app);
    Ok(outcome)
}

/// Records whose copy to use for each listed game path, as `[path, mod]`.
#[tauri::command]
pub async fn set_file_choices(app: AppHandle, state: State<'_, AppState>, choices: Vec<(String, String)>) -> Result<Outcome, String> {
    let outcome = lock(&state).change(|s| {
        for (path, winner) in &choices {
            s.file_choices.insert(path.clone(), winner.clone());
        }
    });
    notify(&app);
    Ok(outcome)
}

/// Forgets the choices between two mods, including file-by-file choices for
/// `paths`; if both stay enabled, the clash is reported again.
#[tauri::command]
pub async fn clear_file_winner(
    app: AppHandle,
    state: State<'_, AppState>,
    a: String,
    b: String,
    paths: Option<Vec<String>>,
) -> Result<Outcome, String> {
    let outcome = lock(&state).change(|s| {
        s.winners.remove(&crate::settings::pair_key(&a, &b));
        for path in paths.iter().flatten() {
            if s.file_choices.get(path).is_some_and(|w| *w == a || *w == b) {
                s.file_choices.remove(path);
            }
        }
    });
    notify(&app);
    Ok(outcome)
}

/// Imports ZIPs after adding a missing version and dependency minimums, as for old mods.
#[tauri::command]
pub async fn import_with_auto_version(app: AppHandle, paths: Vec<String>) -> Result<Outcome, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let temp_dir = {
            let state = app.state::<AppState>();
            let core = lock(&state);
            core.deploy.data_dir.as_ref().map(|d| d.join("tmp"))
        };
        let Some(temp_dir) = temp_dir else {
            return Outcome::rejected("The mod library isn't available.", None::<String>);
        };
        if let Err(e) = std::fs::create_dir_all(&temp_dir) {
            return Outcome::rejected("Couldn't prepare the mods:", Some(e.to_string()));
        }
        let mut upgraded = Vec::new();
        let mut errors = Vec::new();
        for (i, path) in paths.iter().map(PathBuf::from).enumerate() {
            let name = path.file_name().map(|n| n.to_string_lossy().into_owned()).unwrap_or_default();
            let out = temp_dir.join(format!("auto-{i}-{name}"));
            match legacy::upgrade_zip(&path, &out) {
                Ok(()) => upgraded.push(out),
                Err(e) => errors.push(format!("{name}: {e}")),
            }
        }
        let outcome = if errors.is_empty() {
            import_paths(&app, upgraded.clone())
        } else {
            Outcome::rejected("Nothing was imported:", errors)
        };
        for file in upgraded {
            let _ = std::fs::remove_file(file);
        }
        outcome
    })
    .await
    .map_err(|e| e.to_string())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::{temp_dir, write_mod};

    fn core_with(dir: &std::path::Path, enabled: &[&str]) -> Core {
        let mut core = Core { library_dir: Some(dir.to_path_buf()), ..Default::default() };
        core.state.enabled = enabled.iter().map(|s| s.to_string()).collect();
        core.rescan();
        core
    }

    #[test]
    fn bundled_default_mods_are_added_and_enabled_once() {
        let root = temp_dir("bundled");
        let (library, core_dir, default_dir) = (root.join("library"), root.join("core"), root.join("default"));
        for dir in [&library, &core_dir, &default_dir] {
            std::fs::create_dir_all(dir).unwrap();
        }
        write_mod(&core_dir, "v1.zip", "v1", "0.1.0", &[("c-api1", "0.1.0")], "x");
        write_mod(&core_dir, "c-api1.zip", "c-api1", "0.1.0", &[], "x");
        write_mod(&core_dir, "unused.zip", "unused", "0.1.0", &[], "x");
        write_mod(&default_dir, "Invasion.zip", "Invasion", "0.1.0", &[("v1", "0.0.0")], "x");
        let mut core = core_with(&library, &[]);
        core.state_file = Some(root.join("state.json"));

        import_bundled_from(&mut core, &core_dir, &default_dir);
        assert_eq!(core.library.packages.len(), 4);
        let enabled: Vec<&str> = core.state.enabled.iter().map(String::as_str).collect();
        assert_eq!(enabled, vec!["Invasion", "c-api1", "v1"]);

        // The player turns it off; a restart doesn't turn it back on.
        core.state.enabled.remove("Invasion");
        import_bundled_from(&mut core, &core_dir, &default_dir);
        assert!(!core.state.enabled.contains("Invasion"));
    }

    #[test]
    fn package_file_names_are_plain() {
        assert_eq!(package_file_name("omp-airborne-trooper-1.0.0.zip"), "omp-airborne-trooper-1.0.0.zip");
        assert_eq!(package_file_name("../../evil name"), "evil-name.zip");
        assert_eq!(package_file_name("C:\\x\\y.zip"), "C--x-y.zip");
        assert_eq!(package_file_name(""), "mod.zip");
    }

    #[test]
    fn enables_a_new_core_mod_an_updated_one_needs() {
        let root = temp_dir("bundled-new-dep");
        let (library, core_dir, default_dir) = (root.join("library"), root.join("core"), root.join("default"));
        for dir in [&library, &core_dir, &default_dir] {
            std::fs::create_dir_all(dir).unwrap();
        }
        // Installed before: v1 0.1.0, enabled.
        write_mod(&library, "v1-0.1.0.zip", "v1", "0.1.0", &[("c-api1", "0.1.0")], "old");
        write_mod(&library, "c-api1.zip", "c-api1", "0.1.0", &[], "x");
        let mut core = core_with(&library, &["v1", "c-api1"]);
        core.state_file = Some(root.join("state.json"));
        // The new release: v1 0.2.0 needs asset-replacement, which is new.
        write_mod(&core_dir, "v1.zip", "v1", "0.2.0", &[("c-api1", "0.1.0"), ("asset-replacement", "1.0.0")], "new");
        write_mod(&core_dir, "c-api1.zip", "c-api1", "0.1.0", &[], "x");
        write_mod(&core_dir, "asset-replacement.zip", "asset-replacement", "1.0.0", &[], "x");
        write_mod(&core_dir, "unused.zip", "unused", "0.1.0", &[], "x");

        import_bundled_from(&mut core, &core_dir, &default_dir);
        let enabled: Vec<&str> = core.state.enabled.iter().map(String::as_str).collect();
        assert_eq!(enabled, vec!["asset-replacement", "c-api1", "v1"]);
        assert!(core.selection_for(&core.state).problems.is_empty());

        // An earlier update that left it off is fixed on the next start.
        core.state.enabled.remove("asset-replacement");
        import_bundled_from(&mut core, &core_dir, &default_dir);
        assert!(core.state.enabled.contains("asset-replacement"));
        assert!(!core.state.enabled.contains("unused"));
    }

    #[test]
    fn removes_a_version_nothing_needs() {
        let dir = temp_dir("remove-free");
        write_mod(&dir, "a1.zip", "a", "1.0.0", &[], "x");
        let a2 = write_mod(&dir, "a2.zip", "a", "2.0.0", &[], "x");
        let mut core = core_with(&dir, &["a"]);
        // The enabled mod falls back to 1.0.0, so 2.0.0 can go.
        assert!(core.remove_version("a", Version(2, 0, 0)).ok);
        assert!(!a2.exists());
        assert_eq!(core.library.packages["a"].len(), 1);
    }

    #[test]
    fn keeps_a_version_the_selection_needs() {
        let dir = temp_dir("remove-needed");
        let core_zip = write_mod(&dir, "core.zip", "core", "1.0.0", &[], "x");
        write_mod(&dir, "feature.zip", "feature", "1.0.0", &[("core", "1.0.0")], "x");
        let mut core = core_with(&dir, &["core", "feature"]);
        let outcome = core.remove_version("core", Version(1, 0, 0));
        assert!(!outcome.ok);
        assert!(core_zip.exists());

        // Pinned versions count too.
        core.state.pinned.insert("feature".into(), Version(1, 0, 0));
        assert!(!core.remove_version("feature", Version(1, 0, 0)).ok);

        // A disabled mod can always be removed.
        core.state.enabled.clear();
        core.state.pinned.clear();
        assert!(core.remove_version("core", Version(1, 0, 0)).ok);
        assert!(!core_zip.exists());
    }
}
