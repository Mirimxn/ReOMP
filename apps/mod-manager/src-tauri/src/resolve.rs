//! Picks a version for each enabled mod and checks the set can load together.
//! See plans/mod-manager.md#selection-and-automatic-deployment.

use std::collections::{BTreeMap, BTreeSet};

use serde::Serialize;

use crate::library::Library;
use crate::version::Version;

#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Problem {
    pub mod_id: String,
    pub message: String,
    /// Two mods placing the same files with no choice made yet.
    #[serde(skip)]
    pub pair: Option<(String, String)>,
    /// The files the pair still needs a choice for.
    #[serde(skip)]
    pub paths: Vec<String>,
}

#[derive(Debug, Default)]
pub struct Selection {
    pub selected: BTreeMap<String, Version>,
    pub problems: Vec<Problem>,
    /// Game paths (upper case) each mod gives up to a mod chosen over it.
    pub skipped: BTreeMap<String, BTreeSet<String>>,
    /// Every pair of selected mods (sorted) with the game paths both place,
    /// whether or not a choice was made.
    pub shared: BTreeMap<(String, String), BTreeSet<String>>,
}

fn problem(problems: &mut Vec<Problem>, mod_id: &str, message: String) {
    problems.push(Problem { mod_id: mod_id.to_string(), message, pair: None, paths: Vec::new() });
}

pub fn resolve(library: &Library, enabled: &BTreeSet<String>, pinned: &BTreeMap<String, Version>) -> Selection {
    resolve_with(library, enabled, pinned, &BTreeMap::new(), &BTreeMap::new())
}

/// `winners` holds the player's choices for mods placing the same files
/// (see `settings::pair_key`); `file_choices` holds choices made file by
/// file, which take precedence for their path.
pub fn resolve_with(
    library: &Library,
    enabled: &BTreeSet<String>,
    pinned: &BTreeMap<String, Version>,
    winners: &BTreeMap<String, String>,
    file_choices: &BTreeMap<String, String>,
) -> Selection {
    let mut selection = Selection::default();
    let problems = &mut selection.problems;

    for id in enabled {
        let Some(versions) = library.packages.get(id) else {
            problem(problems, id, format!("{id} is enabled but isn't in the library. Add its ZIP or disable it."));
            continue;
        };
        let version = match pinned.get(id) {
            Some(v) if versions.contains_key(v) => *v,
            Some(v) => {
                problem(problems, id, format!("{id} is pinned to version {v}, which isn't in the library."));
                continue;
            }
            None => match versions.keys().next_back() {
                Some(v) => *v,
                None => continue,
            },
        };
        let entry = &versions[&version];
        if entry.is_conflict() {
            let files: Vec<String> = entry.files.keys().map(|p| p.display().to_string()).collect();
            problem(
                problems,
                id,
                format!("Different ZIPs claim {id} {version}: {}. Remove all but one.", files.join(", ")),
            );
        }
        selection.selected.insert(id.clone(), version);
    }

    // OMP deploys each mod into mods/<id>/, so IDs must stay distinct on Windows.
    let mut folded: BTreeMap<String, Vec<&String>> = BTreeMap::new();
    for id in enabled {
        folded.entry(id.to_lowercase()).or_default().push(id);
    }
    for ids in folded.values().filter(|ids| ids.len() > 1) {
        let names: Vec<&str> = ids.iter().map(|s| s.as_str()).collect();
        for id in &ids[..] {
            problem(
                problems,
                id,
                format!("{} would install into the same folder. Enable only one.", names.join(" and ")),
            );
        }
    }

    for (id, version) in &selection.selected {
        let info = &library.packages[id][version].info;
        for dep in &info.manifest.dependencies {
            match selection.selected.get(&dep.id) {
                Some(found) if *found >= dep.min_version => {}
                Some(found) => problem(
                    problems,
                    id,
                    format!("{id} needs {} {} or newer, but {} {found} is selected.", dep.id, dep.min_version, dep.id),
                ),
                None if library.packages.contains_key(&dep.id) => problem(
                    problems,
                    id,
                    format!("{id} needs {} {} or newer. Enable {}.", dep.id, dep.min_version, dep.id),
                ),
                None => problem(
                    problems,
                    id,
                    format!("{id} needs {} {} or newer, which isn't in the library.", dep.id, dep.min_version),
                ),
            }
        }
    }

    // Two enabled mods can't place the same game file unless the player chose
    // which one is used; the other gives up that file.
    let mut placed: BTreeMap<String, Vec<String>> = BTreeMap::new();
    for (id, version) in &selection.selected {
        for resource in &library.packages[id][version].info.manifest.resources {
            placed.entry(resource.path.clone()).or_default().push(id.clone());
        }
    }
    let mut undecided: BTreeMap<(String, String), Vec<String>> = BTreeMap::new();
    for (path, owners) in placed.iter().filter(|(_, owners)| owners.len() > 1) {
        for (i, a) in owners.iter().enumerate() {
            for b in &owners[i + 1..] {
                let pair = if a <= b { (a.clone(), b.clone()) } else { (b.clone(), a.clone()) };
                selection.shared.entry(pair).or_default().insert(path.clone());
            }
        }
        let wins_all = file_choices.get(path).filter(|w| owners.contains(w)).or_else(|| {
            owners.iter().find(|w| {
                owners.iter().all(|o| o == *w || winners.get(&crate::settings::pair_key(w, o)) == Some(*w))
            })
        });
        match wins_all {
            Some(winner) => {
                for other in owners.iter().filter(|o| *o != winner) {
                    selection.skipped.entry(other.clone()).or_default().insert(path.clone());
                }
            }
            None => {
                for (i, a) in owners.iter().enumerate() {
                    for b in &owners[i + 1..] {
                        let pair = if a <= b { (a.clone(), b.clone()) } else { (b.clone(), a.clone()) };
                        undecided.entry(pair).or_default().push(path.clone());
                    }
                }
            }
        }
    }
    for ((a, b), paths) in undecided {
        let shown: Vec<&str> = paths.iter().take(3).map(String::as_str).collect();
        let more = if paths.len() > 3 { format!(" and {} more", paths.len() - 3) } else { String::new() };
        let count = if paths.len() == 1 { "a file".to_string() } else { format!("{} files", paths.len()) };
        let message = format!("{a} and {b} both replace {count} ({}{more}). Choose which mod's files to use.", shown.join(", "));
        for id in [&a, &b] {
            problems.push(Problem {
                mod_id: id.clone(),
                message: message.clone(),
                pair: Some((a.clone(), b.clone())),
                paths: paths.clone(),
            });
        }
    }

    // Background-character colours: each mod changes its own parts of the one
    // palette file; two mods setting the same part differently can't both apply,
    // and a mod shipping the whole file would undo everyone's changes.
    let mut edits: BTreeMap<String, Vec<crate::palette::Edit>> = BTreeMap::new();
    for (id, version) in &selection.selected {
        let manifest = &library.packages[id][version].info.manifest;
        if !manifest.palette_edits.is_empty() {
            edits.insert(id.clone(), manifest.palette_edits.clone());
        }
    }
    for (a, b, part) in crate::palette::clashes(&edits) {
        let message = format!("{a} and {b} both change the background-character colour {part}. Disable one of them.");
        problem(problems, &a, message.clone());
        problem(problems, &b, message);
    }
    if !edits.is_empty() {
        for (id, version) in &selection.selected {
            if library.packages[id][version].info.manifest.resources.iter().any(|r| r.path == crate::palette::GAME_PATH) {
                let others: Vec<&str> = edits.keys().map(String::as_str).collect();
                problem(
                    problems,
                    id,
                    format!(
                        "{id} replaces the whole background-character palette, which would undo the colour changes of {}. Drop the palette file on the Mod Manager to turn it into a mod of its own changes, then disable {id}.",
                        others.join(", ")
                    ),
                );
            }
        }
    }

    for cycle in find_cycles(library, &selection.selected) {
        let path = cycle.iter().chain(cycle.first()).cloned().collect::<Vec<_>>().join(" → ");
        for id in &cycle {
            problem(problems, id, format!("Dependency cycle: {path}"));
        }
    }

    problems.sort();
    problems.dedup();
    selection
}

fn find_cycles(library: &Library, selected: &BTreeMap<String, Version>) -> Vec<Vec<String>> {
    let edges = |id: &str| -> Vec<String> {
        let info = &library.packages[id][&selected[id]].info;
        info.manifest
            .dependencies
            .iter()
            .filter(|d| selected.contains_key(&d.id))
            .map(|d| d.id.clone())
            .collect()
    };
    let mut done: BTreeSet<String> = BTreeSet::new();
    let mut seen_cycles: BTreeSet<BTreeSet<String>> = BTreeSet::new();
    let mut cycles = Vec::new();

    fn visit(
        id: &str,
        edges: &dyn Fn(&str) -> Vec<String>,
        stack: &mut Vec<String>,
        done: &mut BTreeSet<String>,
        seen: &mut BTreeSet<BTreeSet<String>>,
        cycles: &mut Vec<Vec<String>>,
    ) {
        if let Some(start) = stack.iter().position(|s| s == id) {
            let cycle: Vec<String> = stack[start..].to_vec();
            if seen.insert(cycle.iter().cloned().collect()) {
                cycles.push(cycle);
            }
            return;
        }
        if done.contains(id) {
            return;
        }
        stack.push(id.to_string());
        for next in edges(id) {
            visit(&next, edges, stack, done, seen, cycles);
        }
        stack.pop();
        done.insert(id.to_string());
    }

    for id in selected.keys() {
        visit(id, &edges, &mut Vec::new(), &mut done, &mut seen_cycles, &mut cycles);
    }
    cycles
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::library::scan;
    use crate::testutil::{temp_dir, write_mod};

    fn set(ids: &[&str]) -> BTreeSet<String> {
        ids.iter().map(|s| s.to_string()).collect()
    }

    #[test]
    fn picks_the_highest_version_unless_pinned() {
        let dir = temp_dir("resolve-pick");
        write_mod(&dir, "a1.zip", "a", "1.0.0", &[], "x");
        write_mod(&dir, "a2.zip", "a", "1.10.0", &[], "x");
        let library = scan(&dir).unwrap();
        let s = resolve(&library, &set(&["a"]), &BTreeMap::new());
        assert_eq!(s.selected["a"], Version(1, 10, 0));
        assert!(s.problems.is_empty());

        let pinned = BTreeMap::from([("a".to_string(), Version(1, 0, 0))]);
        assert_eq!(resolve(&library, &set(&["a"]), &pinned).selected["a"], Version(1, 0, 0));

        let missing = BTreeMap::from([("a".to_string(), Version(3, 0, 0))]);
        assert_eq!(resolve(&library, &set(&["a"]), &missing).problems.len(), 1);
    }

    #[test]
    fn a_newer_major_version_satisfies_a_minimum() {
        let dir = temp_dir("resolve-major");
        write_mod(&dir, "core.zip", "core", "2.0.0", &[], "x");
        write_mod(&dir, "feature.zip", "feature", "1.0.0", &[("core", "1.2.0")], "x");
        let library = scan(&dir).unwrap();
        assert!(resolve(&library, &set(&["core", "feature"]), &BTreeMap::new()).problems.is_empty());
    }

    #[test]
    fn blocks_missing_old_and_disabled_dependencies() {
        let dir = temp_dir("resolve-missing");
        write_mod(&dir, "core.zip", "core", "1.0.0", &[], "x");
        write_mod(&dir, "needs-new.zip", "needs-new", "1.0.0", &[("core", "1.5.0")], "x");
        write_mod(&dir, "needs-gone.zip", "needs-gone", "1.0.0", &[("gone", "1.0.0")], "x");
        let library = scan(&dir).unwrap();

        let s = resolve(&library, &set(&["needs-new"]), &BTreeMap::new());
        assert!(s.problems[0].message.contains("Enable core"), "{:?}", s.problems);

        let s = resolve(&library, &set(&["core", "needs-new"]), &BTreeMap::new());
        assert!(s.problems[0].message.contains("1.5.0 or newer"), "{:?}", s.problems);

        let s = resolve(&library, &set(&["needs-gone"]), &BTreeMap::new());
        assert!(s.problems[0].message.contains("isn't in the library"), "{:?}", s.problems);
    }

    #[test]
    fn blocks_two_mods_replacing_the_same_file() {
        use crate::testutil::{write_zip, Entry};
        let dir = temp_dir("resolve-assets");
        for (id, path) in [("red", "chars/a.ghg"), ("blue", "CHARS/A.GHG"), ("other", "chars/b.ghg")] {
            let json = format!(
                r#"{{"name":"{id}","version":"1.0.0","dependencies":[],"actions":[{{"typeId":"v1/register-custom-resource-action","payload":{{"path":"{path}","srcPath":"f"}}}}]}}"#
            );
            let zip = dir.join(format!("{id}.zip"));
            let (manifest, file) = (format!("{id}/mod.json"), format!("{id}/f"));
            write_zip(&zip, &[Entry::File(&manifest, json.as_bytes()), Entry::File(&file, b"x")]);
            crate::testutil::make_settled(&zip);
        }
        let library = scan(&dir).unwrap();
        let s = resolve(&library, &set(&["red", "blue", "other"]), &BTreeMap::new());
        assert_eq!(s.problems.len(), 2, "{:?}", s.problems);
        assert!(s.problems[0].message.contains("CHARS/A.GHG"));
        assert_eq!(s.problems[0].pair, Some(("blue".to_string(), "red".to_string())));
        assert!(resolve(&library, &set(&["red", "other"]), &BTreeMap::new()).problems.is_empty());

        // Choosing red over blue settles it; blue gives up the file.
        let winners = BTreeMap::from([(crate::settings::pair_key("red", "blue"), "red".to_string())]);
        let s = resolve_with(&library, &set(&["red", "blue", "other"]), &BTreeMap::new(), &winners, &BTreeMap::new());
        assert!(s.problems.is_empty(), "{:?}", s.problems);
        assert_eq!(s.skipped["blue"], BTreeSet::from(["CHARS/A.GHG".to_string()]));
        assert!(!s.skipped.contains_key("red"));
        assert_eq!(s.shared[&("blue".to_string(), "red".to_string())], BTreeSet::from(["CHARS/A.GHG".to_string()]));
    }

    #[test]
    fn blocks_clashing_background_colours_and_whole_palettes() {
        use crate::testutil::{write_zip, Entry};
        let dir = temp_dir("resolve-palette");
        let set_action = |apply: bool| {
            format!(r#"{{"typeId":"population-palette/set","payload":{{"character":"City_Clone_Couruscant","element":"Arms","apply":{apply}}}}}"#)
        };
        let whole = r#"{"typeId":"asset-replacement/register","payload":{"path":"systems/population/characterpalettedata.prj","srcPath":"f"}}"#.to_string();
        for (id, action) in [("off", set_action(false)), ("same", set_action(false)), ("on", set_action(true)), ("whole", whole)] {
            let json = format!(r#"{{"name":"{id}","version":"1.0.0","dependencies":[],"actions":[{action}]}}"#);
            let zip = dir.join(format!("{id}.zip"));
            let (manifest, file) = (format!("{id}/mod.json"), format!("{id}/f"));
            write_zip(&zip, &[Entry::File(&manifest, json.as_bytes()), Entry::File(&file, b"x")]);
            crate::testutil::make_settled(&zip);
        }
        let library = scan(&dir).unwrap();
        assert!(resolve(&library, &set(&["off", "same"]), &BTreeMap::new()).problems.is_empty());
        let s = resolve(&library, &set(&["off", "on"]), &BTreeMap::new());
        assert_eq!(s.problems.len(), 2, "{:?}", s.problems);
        assert!(s.problems[0].message.contains("City_Clone_Couruscant Arms"));
        assert!(resolve(&library, &set(&["whole"]), &BTreeMap::new()).problems.is_empty());
        let s = resolve(&library, &set(&["off", "whole"]), &BTreeMap::new());
        assert_eq!(s.problems.len(), 1, "{:?}", s.problems);
        assert_eq!(s.problems[0].mod_id, "whole");
    }

    #[test]
    fn file_by_file_choices_override_the_pair_choice() {
        use crate::testutil::{write_zip, Entry};
        let dir = temp_dir("resolve-file-choices");
        for id in ["red", "blue"] {
            let json = format!(
                r#"{{"name":"{id}","version":"1.0.0","dependencies":[],"actions":[{{"typeId":"asset-replacement/register","payload":{{"path":"chars/a.ghg","srcPath":"f"}}}},{{"typeId":"asset-replacement/register","payload":{{"path":"chars/b.ghg","srcPath":"f"}}}}]}}"#
            );
            let zip = dir.join(format!("{id}.zip"));
            let (manifest, file) = (format!("{id}/mod.json"), format!("{id}/f"));
            write_zip(&zip, &[Entry::File(&manifest, json.as_bytes()), Entry::File(&file, b"x")]);
            crate::testutil::make_settled(&zip);
        }
        let library = scan(&dir).unwrap();
        let both = set(&["red", "blue"]);

        // One file chosen, the other still open: only the open file is reported.
        let choices = BTreeMap::from([("CHARS/A.GHG".to_string(), "red".to_string())]);
        let s = resolve_with(&library, &both, &BTreeMap::new(), &BTreeMap::new(), &choices);
        assert_eq!(s.problems.len(), 2, "{:?}", s.problems);
        assert_eq!(s.problems[0].paths, vec!["CHARS/B.GHG".to_string()]);
        assert!(s.problems[0].message.contains("a file (CHARS/B.GHG)"), "{}", s.problems[0].message);

        // Each file from a different mod.
        let choices = BTreeMap::from([
            ("CHARS/A.GHG".to_string(), "red".to_string()),
            ("CHARS/B.GHG".to_string(), "blue".to_string()),
        ]);
        let s = resolve_with(&library, &both, &BTreeMap::new(), &BTreeMap::new(), &choices);
        assert!(s.problems.is_empty(), "{:?}", s.problems);
        assert_eq!(s.skipped["blue"], BTreeSet::from(["CHARS/A.GHG".to_string()]));
        assert_eq!(s.skipped["red"], BTreeSet::from(["CHARS/B.GHG".to_string()]));

        // "All of red" with one file kept from blue.
        let winners = BTreeMap::from([(crate::settings::pair_key("red", "blue"), "red".to_string())]);
        let choices = BTreeMap::from([("CHARS/B.GHG".to_string(), "blue".to_string())]);
        let s = resolve_with(&library, &both, &BTreeMap::new(), &winners, &choices);
        assert!(s.problems.is_empty(), "{:?}", s.problems);
        assert_eq!(s.skipped["blue"], BTreeSet::from(["CHARS/A.GHG".to_string()]));
        assert_eq!(s.skipped["red"], BTreeSet::from(["CHARS/B.GHG".to_string()]));

        // A choice for a mod that doesn't place the file is ignored.
        let choices = BTreeMap::from([("CHARS/A.GHG".to_string(), "other".to_string())]);
        let s = resolve_with(&library, &both, &BTreeMap::new(), &BTreeMap::new(), &choices);
        assert_eq!(s.problems[0].paths.len(), 2);
    }

    #[test]
    fn blocks_cycles_conflicts_and_folder_clashes() {
        let dir = temp_dir("resolve-cycle");
        write_mod(&dir, "a.zip", "a", "1.0.0", &[("b", "1.0.0")], "x");
        write_mod(&dir, "b.zip", "b", "1.0.0", &[("a", "1.0.0")], "x");
        write_mod(&dir, "c1.zip", "c", "1.0.0", &[], "one");
        write_mod(&dir, "c2.zip", "c", "1.0.0", &[], "two");
        write_mod(&dir, "upper.zip", "Thing", "1.0.0", &[], "x");
        write_mod(&dir, "lower.zip", "thing", "1.0.0", &[], "x");
        let library = scan(&dir).unwrap();

        let s = resolve(&library, &set(&["a", "b"]), &BTreeMap::new());
        assert_eq!(s.problems.len(), 2);
        assert!(s.problems.iter().all(|p| p.message.starts_with("Dependency cycle")));

        assert_eq!(resolve(&library, &set(&["c"]), &BTreeMap::new()).problems.len(), 1);
        assert_eq!(resolve(&library, &set(&["Thing", "thing"]), &BTreeMap::new()).problems.len(), 2);
        assert!(resolve(&library, &set(&["Thing"]), &BTreeMap::new()).problems.is_empty());
    }
}
