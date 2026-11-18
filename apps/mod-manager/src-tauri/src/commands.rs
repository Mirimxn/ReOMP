//! Tauri commands for the Mods screen. The UI calls these; all filesystem
//! work and validation stays here.

use std::collections::BTreeSet;
use std::path::PathBuf;
use std::sync::{Mutex, MutexGuard};
use std::thread;

use serde::Serialize;
use tauri::{AppHandle, Emitter, Manager, State};
use tauri_plugin_dialog::DialogExt;

use crate::library::{self, Library};
use crate::resolve::{self, Problem};
use crate::settings::{self, ManagerState};
use crate::version::Version;
use crate::watch;

pub const LIBRARY_CHANGED: &str = "library-changed";

#[derive(Default)]
pub struct Core {
    library_dir: Option<PathBuf>,
    library_error: Option<String>,
    state_file: Option<PathBuf>,
    state: ManagerState,
    library: Library,
    watcher: Option<watch::Watcher>,
    watcher_error: Option<String>,
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

    fn selection_for(&self, state: &ManagerState) -> resolve::Selection {
        resolve::resolve(&self.library, &state.enabled, &state.pinned)
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
            return Outcome::rejected("That change was blocked, so nothing changed:", added.into_iter().map(|p| p.message));
        }
        let Some(file) = self.state_file.clone() else {
            return Outcome::rejected("The manager's settings aren't available.", None::<String>);
        };
        if let Err(e) = settings::save_state(&file, &proposed) {
            return Outcome::rejected("Couldn't save the change:", Some(e));
        }
        self.state = proposed;
        Outcome::ok(Vec::new())
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Outcome {
    ok: bool,
    title: String,
    details: Vec<String>,
}

impl Outcome {
    fn ok(details: Vec<String>) -> Self {
        Outcome { ok: true, title: String::new(), details }
    }
    fn rejected(title: &str, details: impl IntoIterator<Item = String>) -> Self {
        Outcome { ok: false, title: title.to_string(), details: details.into_iter().collect() }
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
pub struct LibraryView {
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
                problems: selection.problems.iter().filter(|p| &p.mod_id == id).map(|p| p.message.clone()).collect(),
            }
        })
        .collect();
    LibraryView {
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

fn lock(state: &AppState) -> MutexGuard<'_, Core> {
    state.0.lock().unwrap_or_else(|poisoned| poisoned.into_inner())
}

fn notify(app: &AppHandle) {
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
                    core.state_file = Some(loaded.state_file);
                    core.state = loaded.state;
                }
                Err(e) => core.library_error = Some(e),
            },
        }
        start_watcher(app, &mut core);
        core.rescan()
    };
    if copying {
        let app = app.clone();
        thread::spawn(move || watch::refresh_until_settled(&app));
    }
}

/// Called by the watcher thread. Returns true if another check is needed soon.
pub fn rescan_and_notify(app: &AppHandle) -> bool {
    let copying = lock(&app.state::<AppState>()).rescan();
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
        Err(errors) => Outcome::rejected("Nothing was imported:", errors),
    };
    core.rescan();
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
pub async fn set_enabled(app: AppHandle, state: State<'_, AppState>, id: String, enabled: bool) -> Result<Outcome, String> {
    let outcome = lock(&state).change(|s| {
        if enabled {
            s.enabled.insert(id.clone());
        } else {
            s.enabled.remove(&id);
        }
    });
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
