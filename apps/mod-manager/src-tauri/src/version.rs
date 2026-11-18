//! Mod versions in strict `MAJOR.MINOR.PATCH` form.
//! See plans/mod-manager.md#packages-and-versions.

use std::fmt;
use std::str::FromStr;

use serde::{Deserialize, Deserializer, Serialize, Serializer};

/// Three non-negative numeric components, compared numerically.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct Version(pub u64, pub u64, pub u64);

impl Version {
    pub fn parse(text: &str) -> Result<Self, String> {
        let invalid = || format!("\"{text}\" is not a MAJOR.MINOR.PATCH version such as 1.0.0");
        let parts: Vec<&str> = text.split('.').collect();
        if parts.len() != 3 {
            return Err(invalid());
        }
        let mut numbers = [0u64; 3];
        for (number, part) in numbers.iter_mut().zip(&parts) {
            let digits_only = !part.is_empty() && part.bytes().all(|b| b.is_ascii_digit());
            if !digits_only || (part.len() > 1 && part.starts_with('0')) {
                return Err(invalid());
            }
            *number = part.parse().map_err(|_| invalid())?;
        }
        Ok(Version(numbers[0], numbers[1], numbers[2]))
    }
}

impl fmt::Display for Version {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}.{}.{}", self.0, self.1, self.2)
    }
}

impl FromStr for Version {
    type Err = String;
    fn from_str(s: &str) -> Result<Self, Self::Err> {
        Version::parse(s)
    }
}

impl Serialize for Version {
    fn serialize<S: Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        serializer.collect_str(self)
    }
}

impl<'de> Deserialize<'de> for Version {
    fn deserialize<D: Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        let text = String::deserialize(deserializer)?;
        Version::parse(&text).map_err(serde::de::Error::custom)
    }
}

#[cfg(test)]
mod tests {
    use super::Version;

    #[test]
    fn parses_strict_versions() {
        assert_eq!(Version::parse("1.2.0"), Ok(Version(1, 2, 0)));
        assert_eq!(Version::parse("0.0.0"), Ok(Version(0, 0, 0)));
        assert_eq!(Version::parse("10.20.30"), Ok(Version(10, 20, 30)));
        for bad in ["1.2", "1.2.3.4", "01.0.0", "1.02.0", "1.0.-1", "1.0.0-beta", "", "a.b.c", "1..0", " 1.0.0"] {
            assert!(Version::parse(bad).is_err(), "{bad} should be rejected");
        }
    }

    #[test]
    fn compares_numerically() {
        let min = Version::parse("1.2.0").unwrap();
        for ok in ["1.2.0", "1.10.0", "2.0.0"] {
            assert!(Version::parse(ok).unwrap() >= min, "{ok} satisfies 1.2.0");
        }
        assert!(Version::parse("1.1.9").unwrap() < min);
        assert_eq!(Version(1, 10, 0).to_string(), "1.10.0");
    }
}
