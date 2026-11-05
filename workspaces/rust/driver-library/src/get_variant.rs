use crate::*;

pub fn get_variant() -> Variant
{
    if std::env::var("SteamAppId").is_ok() {
        return Variant::KnownVariant(KnownVariant::Steam);
    }

    if std::env::args().any(|arg| arg.contains("-epicapp")) {
        return Variant::KnownVariant(KnownVariant::EGS);
    }

    return Variant::Unknown;
}
