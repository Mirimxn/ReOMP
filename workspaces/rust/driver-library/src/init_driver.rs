use std::ffi::c_void;
use std::ptr::null_mut;

use winapi::um::libloaderapi::GetModuleHandleW;

use crate::*;

pub fn init_driver()
{
    let known_variant = match get_variant() {
        Variant::KnownVariant(known_variant) => known_variant,
        _ => return,
    };

    read_driver_config();

    unsafe { PROCESS_EXE_MODULE_HANDLE_OPTION = Some(GetModuleHandleW(null_mut()) as *mut c_void) };

    init_driver_debug_console();

    register_entry_hook(known_variant);
}
