#![allow(clippy::missing_safety_doc)]
#![allow(clippy::needless_return)]
#![allow(clippy::not_unsafe_ptr_arg_deref)]

#[no_mangle]
pub unsafe extern "C" fn createOMPLSWTSSCFuncHook(
    omp_lswtss_c_func_hook_original_ptr: *mut std::ffi::c_void,
    omp_lswtss_c_func_hook_detour_ptr: *mut std::ffi::c_void,
) -> *mut std::ffi::c_void
{
    let omp_lswtss_c_func_hook = detour::RawDetour::new(
        omp_lswtss_c_func_hook_original_ptr as *const _,
        omp_lswtss_c_func_hook_detour_ptr as *const _,
    )
    .unwrap();

    return Box::into_raw(Box::new(omp_lswtss_c_func_hook)) as *mut _;
}

#[no_mangle]
pub unsafe extern "C" fn enableOMPLSWTSSCFuncHook(
    omp_lswtss_c_func_hook_handle: *mut std::ffi::c_void
) -> *mut std::ffi::c_void
{
    let omp_lswtss_c_func_hook_handle =
        omp_lswtss_c_func_hook_handle as *mut detour::RawDetour;

    (*omp_lswtss_c_func_hook_handle)
        .enable()
        .unwrap();

    return (*omp_lswtss_c_func_hook_handle).trampoline() as *const _ as *mut _;
}

#[no_mangle]
pub unsafe extern "C" fn disableOMPLSWTSSCFuncHook(
    omp_lswtss_c_func_hook_handle: *mut std::ffi::c_void
)
{
    let omp_lswtss_c_func_hook_handle =
        omp_lswtss_c_func_hook_handle as *mut detour::RawDetour;

    (*omp_lswtss_c_func_hook_handle)
        .disable()
        .unwrap();
}

#[no_mangle]
pub unsafe extern "C" fn destroyOMPLSWTSSCFuncHook(
    omp_lswtss_c_func_hook_handle: *mut std::ffi::c_void
)
{
    let omp_lswtss_c_func_hook_handle =
        Box::from_raw(omp_lswtss_c_func_hook_handle as *mut detour::RawDetour);

    omp_lswtss_c_func_hook_handle
        .disable()
        .unwrap();
}
