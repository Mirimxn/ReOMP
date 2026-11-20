//! Keeps the enabled game folders in step with the selected mods.
//! One worker thread runs every deployment, so changes never overlap.
//! See plans/mod-manager.md#selection-and-automatic-deployment.

use std::collections::BTreeMap;
use std::fs;
use std::path::{Path, PathBuf};
use std::sync::mpsc::{self, RecvTimeoutError, Sender};
use std::thread;
use std::time::Duration;

use serde::Serialize;
use tauri::{AppHandle, Manager};

use crate::commands::{lock, notify, AppState, Core};
use crate::deploy::{self, DeployMod, Deployment, Payload, Receipt, Receipts, TargetPlan};
use crate::resolve;
use crate::settings;
use crate::targets::{self, TargetKind};

const POLL: Duration = Duration::from_secs(3);
pub const JOURNAL_FILE: &str = "deploy-journal.log";
pub const RECEIPTS_FILE: &str = "receipts.json";

#[derive(Debug, Clone, Default, PartialEq, Serialize)]
#[serde(tag = "state", rename_all = "camelCase")]
pub enum DeployStatus {
    #[default]
    Idle,
    Working,
    Waiting { games: Vec<String> },
    Blocked { reasons: Vec<String> },
    Failed { title: String, details: Vec<String> },
}

/// Deployment state kept in `Core`.
#[derive(Default)]
pub struct DeployState {
    pub status: DeployStatus,
    /// Last successful deployment per target key.
    pub receipts: BTreeMap<String, Receipt>,
    pub payload: Option<Result<Payload, String>>,
    pub data_dir: Option<PathBuf>,
    /// An earlier change couldn't be undone; nothing else runs until it is.
    pub recovery_error: Option<String>,
    /// The request that failed. It isn't retried automatically.
    failed_request: Option<String>,
    retry: bool,
    kick: Option<Sender<()>>,
}

impl DeployState {
    pub fn kick(&self) {
        if let Some(tx) = &self.kick {
            let _ = tx.send(());
        }
    }

    pub fn request_retry(&mut self) {
        self.retry = true;
        self.failed_request = None;
        self.kick();
    }
}

pub fn load_receipts(data_dir: &Path) -> Result<BTreeMap<String, Receipt>, String> {
    let file = data_dir.join(RECEIPTS_FILE);
    if !file.exists() {
        return Ok(BTreeMap::new());
    }
    let text = fs::read_to_string(&file).map_err(|e| format!("Can't read {}: {e}", file.display()))?;
    serde_json::from_str(&text).map_err(|e| format!("{} isn't valid: {e}", file.display()))
}

fn save_receipts(data_dir: &Path, changes: &Receipts) -> Result<(), String> {
    let mut all = load_receipts(data_dir)?;
    for (key, receipt) in changes {
        match receipt {
            Some(r) => all.insert(key.clone(), r.clone()),
            None => all.remove(key),
        };
    }
    settings::write_json(&data_dir.join(RECEIPTS_FILE), &all)
}

/// Finishes or undoes a change interrupted last time. Runs before any new deployment.
pub fn recover(deploy: &mut DeployState) {
    let Some(data_dir) = deploy.data_dir.clone() else { return };
    let result = deploy::recover(&data_dir.join(JOURNAL_FILE), |r| save_receipts(&data_dir, r))
        .and_then(|_| load_receipts(&data_dir));
    match result {
        Ok(receipts) => {
            deploy.receipts = receipts;
            deploy.recovery_error = None;
        }
        Err(e) => deploy.recovery_error = Some(e),
    }
}

pub fn start_worker(app: AppHandle) -> Sender<()> {
    let (tx, rx) = mpsc::channel::<()>();
    thread::spawn(move || {
        let mut poll = false;
        loop {
            let woke = if poll {
                !matches!(rx.recv_timeout(POLL), Err(RecvTimeoutError::Disconnected))
            } else {
                rx.recv().is_ok()
            };
            if !woke {
                break;
            }
            while rx.try_recv().is_ok() {}
            poll = loop {
                match run_once(&app) {
                    Step::Again => continue,
                    Step::Poll => break true,
                    Step::Idle => break false,
                }
            };
        }
    });
    tx
}

enum Step {
    Idle,
    Poll,
    Again,
}

struct Spec {
    kind: TargetKind,
    game_dir: PathBuf,
    want: bool,
    previous: Option<Receipt>,
}

struct Job {
    specs: Vec<Spec>,
    payload: Option<Payload>,
    mods: Vec<DeployMod>,
    data_dir: PathBuf,
    fingerprint: String,
}

enum Prepared {
    Nothing(DeployStatus),
    Wait(DeployStatus),
    Run(Job),
}

fn prepare(core: &mut Core) -> Prepared {
    let deploy = &core.deploy;
    if let Some(error) = &deploy.recovery_error {
        return Prepared::Nothing(DeployStatus::Failed {
            title: "An earlier change to the game files couldn't be undone. Press Retry to try again.".into(),
            details: vec![error.clone()],
        });
    }
    let Some(data_dir) = deploy.data_dir.clone() else { return Prepared::Nothing(DeployStatus::Idle) };

    let mut specs = Vec::new();
    let mut reasons = Vec::new();
    for kind in TargetKind::ALL {
        let settings = core.state.targets.get(&kind).cloned().unwrap_or_default();
        let previous = deploy.receipts.get(kind.key()).cloned();
        let wanted_dir = match (&settings.path, settings.enabled) {
            (Some(path), true) => match targets::validate_game_dir(path) {
                Ok(dir) => Some(dir),
                Err(e) => {
                    reasons.push(format!("{}: {e}", kind.label()));
                    continue;
                }
            },
            _ => None,
        };
        match (wanted_dir, previous) {
            (None, None) => {}
            (None, Some(previous)) => {
                specs.push(Spec { kind, game_dir: previous.game_dir.clone(), want: false, previous: Some(previous) })
            }
            (Some(dir), Some(previous)) if !targets::same_dir(&dir, &previous.game_dir) => {
                // The folder changed: remove from the old one, install into the new one.
                specs.push(Spec { kind, game_dir: previous.game_dir.clone(), want: false, previous: Some(previous) });
                specs.push(Spec { kind, game_dir: dir, want: true, previous: None });
            }
            (Some(dir), previous) => specs.push(Spec { kind, game_dir: dir, want: true, previous }),
        }
    }

    let installing = specs.iter().any(|s| s.want);
    let mut mods = Vec::new();
    let mut payload = None;
    if installing {
        let selection = resolve::resolve(&core.library, &core.state.enabled, &core.state.pinned);
        reasons.extend(selection.problems.iter().map(|p| p.message.clone()));
        for (id, version) in &selection.selected {
            if let Some(entry) = core.library.version(id, *version) {
                if let Some((zip, sha)) = entry.files.iter().next() {
                    mods.push(DeployMod {
                        id: id.clone(),
                        version: *version,
                        zip: zip.clone(),
                        zip_sha256: sha.clone(),
                        resources: entry.info.manifest.resources.clone(),
                    });
                }
            }
        }
        match &deploy.payload {
            Some(Ok(p)) => payload = Some(p.clone()),
            Some(Err(e)) => reasons.push(format!(
                "This copy of the Mod Manager doesn't include OMP, so it can't install into the game ({e})."
            )),
            None => reasons.push("This copy of the Mod Manager doesn't include OMP, so it can't install into the game.".into()),
        }
    }
    if !reasons.is_empty() {
        reasons.push("The game keeps what was installed last time until this is fixed.".into());
        return Prepared::Nothing(DeployStatus::Blocked { reasons });
    }

    let satisfied = specs.iter().all(|s| {
        let want = s.want.then(|| (payload.as_ref().expect("payload checked"), &mods[..]));
        TargetPlan { label: String::new(), game_dir: s.game_dir.clone(), want, previous: s.previous.clone() }
            .is_satisfied()
    });
    if satisfied {
        return Prepared::Nothing(DeployStatus::Idle);
    }

    let fingerprint = serde_json::json!({
        "targets": specs.iter().map(|s| (s.kind.key(), s.game_dir.clone(), s.want)).collect::<Vec<_>>(),
        "payload": payload.as_ref().map(|p| p.version.clone()),
        "mods": mods.iter().map(|m| (m.id.clone(), m.zip_sha256.clone())).collect::<Vec<_>>(),
    })
    .to_string();
    let deploy = &mut core.deploy;
    if deploy.failed_request.as_deref() == Some(fingerprint.as_str()) && !deploy.retry {
        return Prepared::Nothing(deploy.status.clone());
    }
    deploy.retry = false;

    let dirs: Vec<PathBuf> = specs.iter().map(|s| s.game_dir.clone()).collect();
    let busy = targets::running_in(&dirs);
    if !busy.is_empty() {
        let games = specs
            .iter()
            .filter(|s| busy.iter().any(|b| targets::same_dir(b, &s.game_dir)))
            .map(|s| s.kind.label().to_string())
            .collect();
        return Prepared::Wait(DeployStatus::Waiting { games });
    }
    Prepared::Run(Job { specs, payload, mods, data_dir, fingerprint })
}

fn execute(job: &Job) -> Result<Receipts, (DeployStatus, bool)> {
    let failed = |title: &str, details: Vec<String>| DeployStatus::Failed { title: title.into(), details };
    let journal = job.data_dir.join(JOURNAL_FILE);
    let mut deployment = Deployment::begin(&journal)
        .map_err(|e| (failed("Couldn't start updating the game files.", vec![e]), false))?;
    let mut results = Receipts::new();
    for spec in &job.specs {
        let plan = TargetPlan {
            label: spec.kind.label().into(),
            game_dir: spec.game_dir.clone(),
            want: if spec.want { job.payload.as_ref().map(|p| (p, &job.mods[..])) } else { None },
            previous: spec.previous.clone(),
        };
        match deployment.apply(&plan) {
            Ok(receipt) => {
                results.insert(spec.kind.key().into(), receipt);
            }
            Err(error) => {
                return Err(match deployment.rollback() {
                    Ok(()) => (failed("Couldn't update the game files, so nothing was changed.", vec![error]), false),
                    Err(undo) => (
                        failed("Couldn't update the game files, and undoing the change failed too.", vec![error, undo]),
                        true,
                    ),
                });
            }
        }
    }
    let data_dir = job.data_dir.clone();
    deployment
        .commit(results.clone(), |r| save_receipts(&data_dir, r))
        .map_err(|e| (failed("The game files were updated but the change couldn't be finished.", vec![e]), true))?;
    Ok(results)
}

fn run_once(app: &AppHandle) -> Step {
    let state = app.state::<AppState>();
    let job = {
        let mut core = lock(&state);
        match prepare(&mut core) {
            Prepared::Nothing(status) => {
                core.deploy.status = status;
                drop(core);
                notify(app);
                return Step::Idle;
            }
            Prepared::Wait(status) => {
                core.deploy.status = status;
                drop(core);
                notify(app);
                return Step::Poll;
            }
            Prepared::Run(job) => {
                core.deploy.status = DeployStatus::Working;
                job
            }
        }
    };
    notify(app);

    let result = execute(&job);

    let mut core = lock(&state);
    let step = match result {
        Ok(results) => {
            for (key, receipt) in results {
                match receipt {
                    Some(r) => core.deploy.receipts.insert(key, r),
                    None => core.deploy.receipts.remove(&key),
                };
            }
            core.deploy.failed_request = None;
            core.deploy.status = DeployStatus::Idle;
            // Settings may have changed while files were being copied.
            Step::Again
        }
        Err((status, needs_recovery)) => {
            if needs_recovery {
                recover(&mut core.deploy);
                if let Some(error) = core.deploy.recovery_error.clone() {
                    core.deploy.status = DeployStatus::Failed {
                        title: "An earlier change to the game files couldn't be undone. Press Retry to try again.".into(),
                        details: vec![error],
                    };
                } else {
                    core.deploy.status = status;
                }
            } else {
                core.deploy.status = status;
            }
            core.deploy.failed_request = Some(job.fingerprint.clone());
            Step::Idle
        }
    };
    drop(core);
    notify(app);
    step
}

/// Called once at startup, after settings load.
pub fn init(app: &AppHandle, core: &mut Core) {
    core.deploy.payload = Some(
        app.path()
            .resource_dir()
            .map_err(|e| e.to_string())
            .and_then(|dir| Payload::load(&dir.join("omp-payload"))),
    );
    recover(&mut core.deploy);
    core.deploy.kick = Some(start_worker(app.clone()));
    core.deploy.kick();
}
