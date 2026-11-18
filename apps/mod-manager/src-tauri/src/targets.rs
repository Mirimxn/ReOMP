//! Game installations ("targets"): discovery, validation and the running check.
//! See plans/mod-manager.md#game-targets-and-omp-payload.

use std::fs;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

pub const GAME_EXE: &str = "LEGOSTARWARSSKYWALKERSAGA_DX11.exe";
const STEAM_APP_ID: &str = "920210";

#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum TargetKind {
    Steam,
    Epic,
}

impl TargetKind {
    pub const ALL: [TargetKind; 2] = [TargetKind::Steam, TargetKind::Epic];

    pub fn label(self) -> &'static str {
        match self {
            TargetKind::Steam => "Steam",
            TargetKind::Epic => "Epic Games",
        }
    }

    pub fn key(self) -> &'static str {
        match self {
            TargetKind::Steam => "steam",
            TargetKind::Epic => "epic",
        }
    }
}

#[derive(Debug, Clone, Default, PartialEq, Serialize, Deserialize)]
pub struct TargetSettings {
    #[serde(default)]
    pub path: Option<PathBuf>,
    #[serde(default)]
    pub enabled: bool,
}

/// Checks the folder holds the game and returns its canonical form.
pub fn validate_game_dir(path: &Path) -> Result<PathBuf, String> {
    if !path.join(GAME_EXE).is_file() {
        return Err(format!(
            "{} doesn't contain {GAME_EXE}. Choose the folder the game is installed in.",
            path.display()
        ));
    }
    let canonical = fs::canonicalize(path).map_err(|e| format!("Can't open {}: {e}", path.display()))?;
    Ok(simplify(canonical))
}

/// Drops Windows' `\\?\` prefix from local drive paths so they read normally.
fn simplify(path: PathBuf) -> PathBuf {
    let text = path.to_string_lossy();
    match text.strip_prefix(r"\\?\") {
        Some(rest) if !rest.starts_with("UNC\\") => PathBuf::from(rest),
        _ => path,
    }
}

pub fn same_dir(a: &Path, b: &Path) -> bool {
    let fold = |p: &Path| {
        let text = p.to_string_lossy().trim_end_matches(['\\', '/']).to_string();
        if cfg!(windows) { text.to_lowercase() } else { text }
    };
    fold(a) == fold(b)
}

pub fn discover(kind: TargetKind) -> Option<PathBuf> {
    match kind {
        TargetKind::Steam => discover_steam(),
        TargetKind::Epic => discover_epic(),
    }
}

/// Reads every value of `key` from Valve KeyValues text (libraryfolders.vdf, .acf).
pub fn vdf_values(text: &str, key: &str) -> Vec<String> {
    let mut tokens = Vec::new();
    let mut chars = text.chars();
    while let Some(c) = chars.next() {
        if c != '"' {
            continue;
        }
        let mut token = String::new();
        while let Some(c) = chars.next() {
            match c {
                '"' => break,
                '\\' => {
                    if let Some(next) = chars.next() {
                        token.push(next);
                    }
                }
                _ => token.push(c),
            }
        }
        tokens.push(token);
    }
    tokens
        .windows(2)
        .filter(|pair| pair[0].eq_ignore_ascii_case(key))
        .map(|pair| pair[1].clone())
        .collect()
}

fn steam_roots() -> Vec<PathBuf> {
    let mut roots = Vec::new();
    #[cfg(windows)]
    {
        use winreg::RegKey;
        use winreg::enums::HKEY_CURRENT_USER;
        if let Ok(key) = RegKey::predef(HKEY_CURRENT_USER).open_subkey(r"Software\Valve\Steam") {
            if let Ok(path) = key.get_value::<String, _>("SteamPath") {
                roots.push(PathBuf::from(path));
            }
        }
        for var in ["ProgramFiles(x86)", "ProgramFiles"] {
            if let Some(base) = std::env::var_os(var) {
                roots.push(PathBuf::from(base).join("Steam"));
            }
        }
    }
    #[cfg(not(windows))]
    if let Some(home) = std::env::var_os("HOME") {
        let home = PathBuf::from(home);
        roots.push(home.join(".steam/steam"));
        roots.push(home.join(".local/share/Steam"));
    }
    roots
}

fn discover_steam() -> Option<PathBuf> {
    for root in steam_roots() {
        let mut libraries = vec![root.clone()];
        if let Ok(text) = fs::read_to_string(root.join("steamapps").join("libraryfolders.vdf")) {
            libraries.extend(vdf_values(&text, "path").into_iter().map(PathBuf::from));
        }
        for library in libraries {
            let steamapps = library.join("steamapps");
            let Ok(manifest) = fs::read_to_string(steamapps.join(format!("appmanifest_{STEAM_APP_ID}.acf"))) else {
                continue;
            };
            if let Some(dir) = vdf_values(&manifest, "installdir").first() {
                if let Ok(found) = validate_game_dir(&steamapps.join("common").join(dir)) {
                    return Some(found);
                }
            }
        }
    }
    None
}

fn discover_epic() -> Option<PathBuf> {
    let data = std::env::var_os("ProgramData")?;
    let manifests = PathBuf::from(data).join(r"Epic\EpicGamesLauncher\Data\Manifests");
    for entry in fs::read_dir(manifests).ok()?.flatten() {
        if !entry.path().extension().is_some_and(|e| e.eq_ignore_ascii_case("item")) {
            continue;
        }
        let Ok(text) = fs::read_to_string(entry.path()) else { continue };
        let Ok(item) = serde_json::from_str::<serde_json::Value>(&text) else { continue };
        if let Some(location) = item.get("InstallLocation").and_then(|v| v.as_str()) {
            if let Ok(found) = validate_game_dir(Path::new(location)) {
                return Some(found);
            }
        }
    }
    None
}

/// Game folders among `dirs` that may have the game running. A game process
/// whose path can't be read counts as running in every folder.
pub fn running_in(dirs: &[PathBuf]) -> Vec<PathBuf> {
    use sysinfo::{ProcessRefreshKind, ProcessesToUpdate, System, UpdateKind};
    let mut system = System::new();
    system.refresh_processes_specifics(
        ProcessesToUpdate::All,
        true,
        ProcessRefreshKind::nothing().with_exe(UpdateKind::Always),
    );
    let mut busy: Vec<PathBuf> = Vec::new();
    for process in system.processes().values() {
        let name = process.name().to_string_lossy();
        // Linux truncates process names to 15 characters.
        let is_game = name.eq_ignore_ascii_case(GAME_EXE)
            || (name.len() >= 15 && GAME_EXE.to_ascii_lowercase().starts_with(&name.to_ascii_lowercase()));
        if !is_game {
            continue;
        }
        let exe_dir = process.exe().and_then(|exe| exe.parent()).map(|dir| {
            fs::canonicalize(dir).map(simplify).unwrap_or_else(|_| dir.to_path_buf())
        });
        for dir in dirs {
            let matches = exe_dir.as_deref().is_none_or(|exe_dir| same_dir(exe_dir, dir));
            if matches && !busy.iter().any(|b| same_dir(b, dir)) {
                busy.push(dir.clone());
            }
        }
    }
    busy
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::testutil::temp_dir;

    #[test]
    fn reads_steam_library_files() {
        let libraries = r#"
"libraryfolders"
{
	"0"	{ "path"	"C:\\Program Files (x86)\\Steam" "apps" { "228980" "1" } }
	"1"	{ "path"	"E:\\SteamLibrary" "apps" { "920210" "1" } }
}"#;
        assert_eq!(vdf_values(libraries, "path"), vec![r"C:\Program Files (x86)\Steam", r"E:\SteamLibrary"]);
        let acf = r#""AppState" { "appid" "920210" "installdir" "LEGO Star Wars - The Skywalker Saga" }"#;
        assert_eq!(vdf_values(acf, "installdir"), vec!["LEGO Star Wars - The Skywalker Saga"]);
    }

    #[test]
    fn validates_the_game_folder() {
        let dir = temp_dir("game-dir");
        assert!(validate_game_dir(&dir).is_err());
        fs::write(dir.join(GAME_EXE), b"exe").unwrap();
        let canonical = validate_game_dir(&dir).unwrap();
        assert!(same_dir(&canonical, &validate_game_dir(&dir.join(".")).unwrap()));
        assert!(!canonical.to_string_lossy().starts_with(r"\\?\"));
    }

    #[test]
    fn running_check_ignores_unrelated_folders() {
        // The test runner isn't the game, so nothing is reported.
        assert!(running_in(&[temp_dir("not-running")]).is_empty());
    }
}
