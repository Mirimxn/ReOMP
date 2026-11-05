use std::ffi::c_ulonglong;

use crate::*;

type Entry = unsafe extern "stdcall" fn() -> c_ulonglong;

static mut ENTRY_HOOK_TRAMPOLINE: Entry = entry_hook_detour;

static mut ENTRY_HOOK_HANDLE_OPTION: Option<detour::RawDetour> = None;

pub fn register_entry_hook(known_variant: KnownVariant)
{
    unsafe {
        let entry_ptr = PROCESS_EXE_MODULE_HANDLE_OPTION
            .as_ref()
            .unwrap()
            .byte_offset(
                match known_variant {
                    KnownVariant::Steam => 0x35da42c,
                    KnownVariant::EGS => 0x35daafc,
                },
            );

        ENTRY_HOOK_HANDLE_OPTION = Some(
            detour::RawDetour::new(
                entry_ptr as *const (),
                entry_hook_detour as *const (),
            )
            .unwrap(),
        );

        ENTRY_HOOK_TRAMPOLINE = std::mem::transmute(
            ENTRY_HOOK_HANDLE_OPTION
                .as_ref()
                .unwrap()
                .trampoline(),
        );

        ENTRY_HOOK_HANDLE_OPTION
            .as_ref()
            .unwrap()
            .enable()
            .unwrap();
    }
}

static mut IS_ENGINE_INITIALIZED: bool = false;

unsafe extern "stdcall" fn entry_hook_detour() -> c_ulonglong
{
    if !IS_ENGINE_INITIALIZED {
        init_engine();

        IS_ENGINE_INITIALIZED = true;
    }

    return ENTRY_HOOK_TRAMPOLINE();
}
