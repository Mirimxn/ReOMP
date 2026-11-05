use crate::*;

pub fn read_driver_config()
{
    let driver_config = serde_json::from_str::<DriverConfig>(
        std::fs::read_to_string(
            std::env::current_dir()
                .unwrap()
                .join("omp-lswtss-driver-config.json"),
        )
        .unwrap()
        .as_str(),
    )
    .unwrap();

    unsafe { DRIVER_CONFIG_OPTION = Some(driver_config) };
}
