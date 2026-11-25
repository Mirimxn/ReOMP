#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod assets;
mod commands;
mod dat;
mod draft;
mod deploy;
mod deployer;
mod legacy;
mod library;
mod package;
mod resolve;
mod settings;
mod targets;
#[cfg(test)]
mod testutil;
mod version;
mod watch;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(commands::AppState::default())
        .manage(commands::AssetState::default())
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
            commands::pick_game_folder,
            commands::set_target_enabled,
            commands::retry_deploy,
            commands::add_legacy_mod,
            commands::remove_version,
            commands::assets_open,
            commands::assets_list,
            commands::assets_search,
            commands::assets_extract,
            commands::draft_pick_file,
            commands::draft_build,
            commands::set_file_winner,
            commands::set_file_choices,
            commands::assets_paths,
            commands::assets_read,
            commands::package_bytes,
            commands::clear_file_winner,
            commands::import_with_auto_version,
        ])
        .run(tauri::generate_context!())
        .expect("failed to run the ReOMP Mod Manager");
}
