//! ReOMP's global config (config/reomp.schema.json) and the manager's saved
//! selection. The inventory itself is always derived from the library folder.

use std::collections::{BTreeMap, BTreeSet};
use std::fs;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use crate::targets::{TargetKind, TargetSettings};
use crate::version::Version;

#[derive(Serialize, Deserialize)]
struct GlobalConfig {
    #[serde(rename = "installedMods")]
    installed_mods: String,
}

/// Shared enabled IDs and version pins.
#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
pub struct ManagerState {
    #[serde(default)]
    pub enabled: BTreeSet<String>,
    #[serde(default, rename = "pinnedVersions")]
    pub pinned: BTreeMap<String, Version>,
    /// Game folders and whether OMP and mods are installed into them.
    #[serde(default)]
    pub targets: BTreeMap<TargetKind, TargetSettings>,
    /// Bundled core mod ZIPs already offered to the library, as `id@version`,
    /// so one the player removes isn't added back.
    #[serde(default, rename = "bundledImported")]
    pub bundled_imported: BTreeSet<String>,
    /// For two mods that place the same files, the one whose files are used.
    /// Keyed by `pair_key`.
    #[serde(default, rename = "fileWinners")]
    pub winners: BTreeMap<String, String>,
    /// Game path (upper case) -> the mod whose copy is used, chosen file by
    /// file. Overrides `winners` for that path.
    #[serde(default, rename = "fileChoices")]
    pub file_choices: BTreeMap<String, String>,
}

/// Order-independent key for two mod IDs.
pub fn pair_key(a: &str, b: &str) -> String {
    if a <= b { format!("{a}|{b}") } else { format!("{b}|{a}") }
}

pub struct Loaded {
    pub data_dir: PathBuf,
    pub library_dir: PathBuf,
    pub state_file: PathBuf,
    pub state: ManagerState,
}

pub const CONFIG_FILE: &str = "reomp.json";
pub const STATE_FILE: &str = "manager-state.json";

/// On first run, creates the library under `data_dir` and records it in the
/// global config. Later runs use the recorded path and report it if unusable.
pub fn load(config_dir: &Path, data_dir: &Path) -> Result<Loaded, String> {
    fs::create_dir_all(config_dir)
        .map_err(|e| format!("Couldn't create the settings folder {}: {e}", config_dir.display()))?;
    fs::create_dir_all(data_dir)
        .map_err(|e| format!("Couldn't create the data folder {}: {e}", data_dir.display()))?;

    let config_file = config_dir.join(CONFIG_FILE);
    let library_dir = if config_file.exists() {
        let text = fs::read_to_string(&config_file)
            .map_err(|e| format!("Couldn't read {}: {e}", config_file.display()))?;
        let config: GlobalConfig = serde_json::from_str(&text)
            .map_err(|e| format!("{} isn't valid: {e}", config_file.display()))?;
        let path = PathBuf::from(&config.installed_mods);
        if config.installed_mods.is_empty() || !path.is_absolute() {
            return Err(format!(
                "installedMods in {} must be an absolute folder path (it is \"{}\").",
                config_file.display(),
                config.installed_mods
            ));
        }
        path
    } else {
        let path = data_dir.join("mods");
        fs::create_dir_all(&path)
            .map_err(|e| format!("Couldn't create the mod library folder {}: {e}", path.display()))?;
        let config = GlobalConfig { installed_mods: path.to_string_lossy().into_owned() };
        write_json(&config_file, &config)?;
        path
    };
    if !library_dir.is_dir() || fs::read_dir(&library_dir).is_err() {
        return Err(format!(
            "The mod library folder {} (set as installedMods in {}) doesn't exist or can't be opened.",
            library_dir.display(),
            config_file.display()
        ));
    }

    let state_file = data_dir.join(STATE_FILE);
    let state = if state_file.exists() {
        let text = fs::read_to_string(&state_file)
            .map_err(|e| format!("Couldn't read {}: {e}", state_file.display()))?;
        serde_json::from_str(&text).map_err(|e| format!("{} isn't valid: {e}", state_file.display()))?
    } else {
        ManagerState::default()
    };
    Ok(Loaded { data_dir: data_dir.to_path_buf(), library_dir, state_file, state })
}

pub fn save_state(file: &Path, state: &ManagerState) -> Result<(), String> {
    write_json(file, state)
}

/// Writes a temporary file and renames it over the target.
pub fn write_json<T: Serialize>(path: &Path, value: &T) -> Result<(), String> {
    let text = serde_json::to_string_pretty(value).map_err(|e| e.to_string())?;
    let temp = path.with_extension("json.tmp");
    fs::write(&temp, text + "\n").map_err(|e| format!("Couldn't save {}: {e}", path.display()))?;
    fs::rename(&temp, path).map_err(|e| {
        let _ = fs::remove_file(&temp);
        format!("Couldn't save {}: {e}", path.display())
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::temp_dir;

    #[test]
    fn first_run_creates_and_records_the_library() {
        let root = temp_dir("settings");
        let (config, data) = (root.join("config"), root.join("data"));
        let loaded = load(&config, &data).unwrap();
        assert_eq!(loaded.library_dir, data.join("mods"));
        assert!(loaded.library_dir.is_dir());
        let recorded: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(config.join(CONFIG_FILE)).unwrap()).unwrap();
        assert_eq!(recorded["installedMods"], data.join("mods").to_string_lossy().into_owned());

        let mut state = ManagerState::default();
        state.enabled.insert("v1".into());
        state.pinned.insert("v1".into(), Version(1, 0, 0));
        state.targets.insert(TargetKind::Steam, TargetSettings { path: Some(root.join("game")), enabled: true });
        save_state(&loaded.state_file, &state).unwrap();
        assert_eq!(load(&config, &data).unwrap().state, state);
    }

    #[test]
    fn reports_a_missing_configured_folder() {
        let root = temp_dir("settings-missing");
        let config = root.join("config");
        fs::create_dir_all(&config).unwrap();
        let gone = root.join("gone");
        let text = serde_json::json!({ "installedMods": gone.to_string_lossy() }).to_string();
        fs::write(config.join(CONFIG_FILE), text).unwrap();
        let error = load(&config, &root.join("data")).err().unwrap();
        assert!(error.contains("doesn't exist"), "{error}");
        assert!(!gone.exists(), "the configured path isn't recreated");
    }
}
