#![allow(clippy::missing_safety_doc)]
#![allow(clippy::needless_return)]
#![allow(clippy::not_unsafe_ptr_arg_deref)]

mod download_dotnet_runtime_if_missing;
mod driver_config;
mod driver_config_option;
mod driver_debug_console;
mod driver_library_dll_main;
mod entry_hook;
mod get_dotnet_runtime_path;
mod get_variant;
mod init_driver;
mod init_engine;
mod known_variant;
mod process_exe_module_handle_option;
mod read_driver_config;
mod variant;

pub use download_dotnet_runtime_if_missing::*;
pub use driver_config::*;
pub use driver_config_option::*;
pub use driver_debug_console::*;
pub use entry_hook::*;
pub use get_dotnet_runtime_path::*;
pub use get_variant::*;
pub use init_driver::*;
pub use init_engine::*;
pub use known_variant::*;
pub use process_exe_module_handle_option::*;
pub use read_driver_config::*;
pub use variant::*;
