use std::path::PathBuf;

pub fn get_dotnet_runtime_path() -> PathBuf
{
    return std::env::current_dir()
        .unwrap()
        .join("omp-lswtss-dotnet-runtime-8.0.2");
}
