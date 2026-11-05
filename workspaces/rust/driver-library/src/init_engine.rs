use crate::*;

pub fn init_engine()
{
    download_dotnet_runtime_if_missing();

    let driver_config = unsafe {
        DRIVER_CONFIG_OPTION
            .as_ref()
            .unwrap()
    };

    let netcore_hostfxr_library_handle = netcorehost::hostfxr::Hostfxr::load_from_path(
        get_dotnet_runtime_path()
            .join("host")
            .join("fxr")
            .join("8.0.2")
            .join("hostfxr.dll"),
    )
    .unwrap();

    let engine_assembly_config_path_as_pdcstring = netcorehost::pdcstring::PdCString::from_os_str(
        std::env::current_dir()
            .unwrap()
            .join(&driver_config.engine_assembly_runtime_config_path),
    )
    .unwrap();

    let netcore_context_handle = netcore_hostfxr_library_handle
        .initialize_for_runtime_config(engine_assembly_config_path_as_pdcstring)
        .unwrap();

    let engine_assembly_path_as_pdcstring = netcorehost::pdcstring::PdCString::from_os_str(
        std::env::current_dir()
            .unwrap()
            .join(&driver_config.engine_assembly_path),
    )
    .unwrap();

    let engine_class_signature = format!(
        "{}, {}",
        driver_config.engine_class_name, driver_config.engine_assembly_name,
    );

    let engine_class_signature_as_pdcstring =
        netcorehost::pdcstring::PdCString::from_os_str(engine_class_signature).unwrap();

    let engine_class_init = netcore_context_handle
        .get_delegate_loader_for_assembly(engine_assembly_path_as_pdcstring)
        .unwrap()
        .get_function_with_default_signature(
            &engine_class_signature_as_pdcstring,
            netcorehost::pdcstr!("Init"),
        )
        .unwrap();

    unsafe {
        engine_class_init(
            std::ptr::null(),
            0,
        )
    };
}
