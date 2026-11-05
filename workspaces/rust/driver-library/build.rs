fn download_dotnet_sdk_if_missing() {
    let dotnet_runtime_dir_path =
        std::path::Path::new(&std::env::var("OUT_DIR").unwrap()).join("dotnet-sdk-8.0.201");

    if dotnet_runtime_dir_path.exists() {
        return;
    }

    let dotnet_runtime_zip_bytes = reqwest::blocking::get("https://download.visualstudio.microsoft.com/download/pr/a98272cb-1559-46f8-9601-16a80827557e/c5cab2b6e195e8a8ee42ea484cca5f80/dotnet-sdk-8.0.201-win-x64.zip")
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
}

pub fn main() {
    download_dotnet_sdk_if_missing();

    println!(
        "cargo:rustc-link-search={}",
        std::path::Path::new(&std::env::var("OUT_DIR").unwrap())
            .join("dotnet-sdk-8.0.201")
            .join("packs")
            .join("Microsoft.NETCore.App.Host.win-x64")
            .join("8.0.2")
            .join("runtimes")
            .join("win-x64")
            .join("native")
            .display()
    );
}
