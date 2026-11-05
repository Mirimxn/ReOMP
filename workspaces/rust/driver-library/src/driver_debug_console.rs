pub fn init_driver_debug_console()
{
    let _ = simplelog::WriteLogger::init(
        simplelog::LevelFilter::Info,
        simplelog::Config::default(),
        std::io::stdout(),
    );

    std::panic::set_hook(
        Box::new(
            |panic_info| {
                log::error!("{}", panic_info);
            },
        ),
    );
}
