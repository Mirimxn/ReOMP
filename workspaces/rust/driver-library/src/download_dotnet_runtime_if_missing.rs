use std::ffi::CString;

use crate::*;

pub fn download_dotnet_runtime_if_missing()
{
    let dotnet_runtime_dir_path = get_dotnet_runtime_path();

    if dotnet_runtime_dir_path.exists() {
        return;
    }

    unsafe {
        let message_box_message_cstr = CString::new("The .NET Runtime needs to be downloaded. The operation will proceed after clicking OK.").unwrap();
        let message_box_title_cstr = CString::new("OMP LSWTSS").unwrap();

        winapi::um::winuser::MessageBoxA(
            std::ptr::null_mut(),
            message_box_message_cstr.as_ptr(),
            message_box_title_cstr.as_ptr(),
            winapi::um::winuser::MB_OK,
        );
    }

    let dotnet_runtime_zip_bytes = reqwest::blocking::get("https://download.visualstudio.microsoft.com/download/pr/8abf4502-4a22-4a2e-bea0-9fe73379d62e/88146c1d41e53e08f9dbc92a217143de/dotnet-runtime-8.0.2-win-x64.zip")
        .unwrap()
        .bytes()
        .unwrap();

    let dotnet_runtime_zip_bytes_reader = std::io::Cursor::new(dotnet_runtime_zip_bytes);
    let mut dotnet_runtime_zip_handle =
        zip::ZipArchive::new(dotnet_runtime_zip_bytes_reader).unwrap();

    for i in 0..dotnet_runtime_zip_handle.len() {
        let mut file = dotnet_runtime_zip_handle
            .by_index(i)
            .unwrap();

        let outpath = dotnet_runtime_dir_path.join(
            file.enclosed_name()
                .unwrap(),
        );

        if (*file.name()).ends_with('/') {
            std::fs::create_dir_all(&outpath).unwrap();
        } else {
            if let Some(p) = outpath.parent() {
                if !p.exists() {
                    std::fs::create_dir_all(p).unwrap();
                }
            }

            let mut outfile = std::fs::File::create(&outpath).unwrap();

            std::io::copy(
                &mut file,
                &mut outfile,
            )
            .unwrap();
        }
    }

    unsafe {
        let message_box_message_cstr = CString::new("The .NET Runtime has been downloaded!").unwrap();
        let message_box_title_cstr = CString::new("OMP LSWTSS").unwrap();

        winapi::um::winuser::MessageBoxA(
            std::ptr::null_mut(),
            message_box_message_cstr.as_ptr(),
            message_box_title_cstr.as_ptr(),
            winapi::um::winuser::MB_OK,
        );
    }
}
