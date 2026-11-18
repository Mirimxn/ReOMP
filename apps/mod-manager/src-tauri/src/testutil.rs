//! Helpers for building fixture ZIPs in tests.

use std::fs::{self, File};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
use std::time::{Duration, SystemTime, UNIX_EPOCH};

use zip::write::SimpleFileOptions;

pub enum Entry<'a> {
    Dir(&'a str),
    File(&'a str, &'a [u8]),
    Link(&'a str, &'a str),
}

pub fn temp_dir(label: &str) -> PathBuf {
    static NEXT: AtomicU64 = AtomicU64::new(0);
    let nanos = SystemTime::now().duration_since(UNIX_EPOCH).unwrap().as_nanos();
    let dir = std::env::temp_dir().join(format!(
        "reomp-test-{label}-{}-{nanos}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    fs::create_dir_all(&dir).unwrap();
    dir
}

pub fn write_zip(path: &Path, entries: &[Entry]) {
    let mut writer = zip::ZipWriter::new(File::create(path).unwrap());
    let options = SimpleFileOptions::default();
    for entry in entries {
        match entry {
            Entry::Dir(name) => writer.add_directory(*name, options).unwrap(),
            Entry::File(name, bytes) => {
                writer.start_file(*name, options).unwrap();
                writer.write_all(bytes).unwrap();
            }
            Entry::Link(name, target) => writer.add_symlink(*name, *target, options).unwrap(),
        }
    }
    writer.finish().unwrap();
}

pub fn manifest(name: &str, version: &str, deps: &[(&str, &str)]) -> String {
    let deps: Vec<String> = deps
        .iter()
        .map(|(id, min)| format!(r#"{{"id":"{id}","minVersion":"{min}"}}"#))
        .collect();
    format!(
        r#"{{"name":"{name}","version":"{version}","actions":[],"dependencies":[{}]}}"#,
        deps.join(",")
    )
}

/// Writes a minimal valid mod ZIP. `extra` changes the bytes without changing the manifest.
pub fn write_mod(dir: &Path, file: &str, id: &str, version: &str, deps: &[(&str, &str)], extra: &str) -> PathBuf {
    let path = dir.join(file);
    let json = manifest(id, version, deps);
    let manifest_path = format!("{id}/mod.json");
    let extra_path = format!("{id}/extra.txt");
    write_zip(
        &path,
        &[Entry::File(&manifest_path, json.as_bytes()), Entry::File(&extra_path, extra.as_bytes())],
    );
    make_settled(&path);
    path
}

/// Backdates a file so the library treats it as fully copied.
pub fn make_settled(path: &Path) {
    let old = SystemTime::now() - Duration::from_secs(60);
    File::options().write(true).open(path).unwrap().set_modified(old).unwrap();
}
