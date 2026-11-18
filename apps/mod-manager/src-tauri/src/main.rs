#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod commands;
mod library;
mod package;
mod resolve;
mod settings;
#[cfg(test)]
mod testutil;
mod version;
mod watch;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(commands::AppState::default())
        .setup(|app| {
            commands::init(app.handle());
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::get_library,
            commands::import_zips,
            commands::pick_and_import,
            commands::set_enabled,
            commands::set_pinned_version,
            commands::retry_watcher,
        ])
        .run(tauri::generate_context!())
        .expect("failed to run the ReOMP Mod Manager");
}
