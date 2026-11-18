//! Watches the library folder so the inventory updates while the app is open.
//! See plans/mod-manager.md#packages-and-versions (step 6). https://docs.rs/notify

use std::path::PathBuf;
use std::sync::mpsc;
use std::thread;
use std::time::Duration;

use notify::{EventKind, RecursiveMode, Watcher as _};
use tauri::AppHandle;

use crate::commands;

const DEBOUNCE: Duration = Duration::from_millis(400);
const RECHECK: Duration = Duration::from_secs(2);
const MAX_RECHECKS: u32 = 150;

/// Keeps the native watcher alive. Dropping it stops the background thread.
pub struct Watcher {
    _inner: notify::RecommendedWatcher,
}

enum Message {
    Changed,
    Failed(String),
}

pub fn start(app: AppHandle, dir: PathBuf) -> Result<Watcher, String> {
    let (tx, rx) = mpsc::channel::<Message>();
    let mut inner = notify::recommended_watcher(move |result: notify::Result<notify::Event>| {
        let message = match result {
            Ok(event) if matches!(event.kind, EventKind::Access(_)) => return,
            Ok(_) => Message::Changed,
            Err(error) => Message::Failed(error.to_string()),
        };
        let _ = tx.send(message);
    })
    .map_err(|e| format!("Couldn't watch the mod library folder: {e}"))?;
    inner
        .watch(&dir, RecursiveMode::NonRecursive)
        .map_err(|e| format!("Couldn't watch the mod library folder {}: {e}", dir.display()))?;

    thread::spawn(move || {
        while let Ok(message) = rx.recv() {
            let mut failure = None;
            if let Message::Failed(error) = message {
                failure = Some(error);
            }
            // Debounce bursts of create/change/rename events into one rescan.
            while let Ok(next) = rx.recv_timeout(DEBOUNCE) {
                if let Message::Failed(error) = next {
                    failure = Some(error);
                }
            }
            if let Some(error) = failure {
                commands::report_watcher_failure(&app, error);
            }
            refresh_until_settled(&app);
        }
    });
    Ok(Watcher { _inner: inner })
}

/// Rescans, and keeps rechecking while a ZIP looks like it is still being copied.
pub fn refresh_until_settled(app: &AppHandle) {
    for _ in 0..MAX_RECHECKS {
        if !commands::rescan_and_notify(app) {
            return;
        }
        thread::sleep(RECHECK);
    }
}
