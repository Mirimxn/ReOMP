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
}

#[derive(Debug, Default)]
pub struct Selection {
    pub selected: BTreeMap<String, Version>,
    pub problems: Vec<Problem>,
}

fn problem(problems: &mut Vec<Problem>, mod_id: &str, message: String) {
    problems.push(Problem { mod_id: mod_id.to_string(), message });
}

pub fn resolve(
    library: &Library,
    enabled: &BTreeSet<String>,
    pinned: &BTreeMap<String, Version>,
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

    // Two enabled mods can't place the same game file.
    let mut placed: BTreeMap<String, Vec<&String>> = BTreeMap::new();
    for (id, version) in &selection.selected {
        for resource in &library.packages[id][version].info.manifest.resources {
            placed.entry(resource.path.to_lowercase()).or_default().push(id);
        }
    }
    for (path, owners) in placed.iter().filter(|(_, owners)| owners.len() > 1) {
        let names: Vec<&str> = owners.iter().map(|s| s.as_str()).collect();
        for id in owners {
            problem(
                problems,
                id,
                format!("{} both replace {}. Enable only one of them.", names.join(" and "), path.to_uppercase()),
            );
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
        assert!(resolve(&library, &set(&["red", "other"]), &BTreeMap::new()).problems.is_empty());
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
