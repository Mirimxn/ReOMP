//! The game's background-character colours, `SYSTEMS/POPULATION/CHARACTERPALETTEDATA.PRJ`.
//! Each character name has one or more palettes; each palette has a colour and
//! an on/off switch per part ("Arms", "Upper Body", "Hat attachment"...). The
//! game paints a switched-on part with its colour, over the character's own.
//!
//! Mods don't ship this shared file. They declare `population-palette/set`
//! actions, and the manager writes one file from the game's original with every
//! enabled mod's changes; the `population-palette` mod serves it.
//! See plans/mod-manager.md#background-character-colours.

use std::collections::BTreeMap;
use std::fs::File;
use std::path::{Path, PathBuf};
use std::sync::Mutex;

use serde::Serialize;
use serde_json::Value;

/// The game path of the palette file, in the game's upper-case form.
pub const GAME_PATH: &str = "SYSTEMS/POPULATION/CHARACTERPALETTEDATA.PRJ";
/// Where the combined file sits inside the provider's mod folder.
pub const MERGED_SRC: &str = "merged/CHARACTERPALETTEDATA.PRJ";
pub const PROVIDER: &str = "population-palette";
pub const SET_ACTION: &str = "population-palette/set";

#[derive(Debug, Clone)]
pub struct Element {
    pub name: String,
    pub colour: [f32; 3],
    pub apply: bool,
    colour_at: usize,
    apply_at: usize,
}

#[derive(Debug, Clone)]
pub struct Collection {
    pub name: String,
    pub palettes: Vec<Vec<Element>>,
}

/// One declared change: a part of a character's palettes (all of them, or one
/// by index) switched on or off, recoloured, or both.
#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Edit {
    pub character: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub palette: Option<u32>,
    pub element: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub apply: Option<bool>,
    #[serde(skip_serializing_if = "Option::is_none", serialize_with = "hex_opt")]
    pub colour: Option<[u8; 3]>,
}

fn hex_opt<S: serde::Serializer>(c: &Option<[u8; 3]>, s: S) -> Result<S::Ok, S::Error> {
    match c {
        Some(c) => s.serialize_str(&hex(*c)),
        None => s.serialize_none(),
    }
}

pub fn hex(c: [u8; 3]) -> String {
    format!("#{:02x}{:02x}{:02x}", c[0], c[1], c[2])
}

impl Edit {
    /// True when both edits can reach the same palette part.
    fn overlaps(&self, other: &Edit) -> bool {
        self.character.eq_ignore_ascii_case(&other.character)
            && self.element.eq_ignore_ascii_case(&other.element)
            && (self.palette.is_none() || other.palette.is_none() || self.palette == other.palette)
    }

    /// True when both edits set the same field to different values.
    pub fn clashes(&self, other: &Edit) -> bool {
        self.overlaps(other)
            && (matches!((self.apply, other.apply), (Some(a), Some(b)) if a != b)
                || matches!((self.colour, other.colour), (Some(a), Some(b)) if a != b))
    }

    pub fn describe(&self) -> String {
        let which = self.palette.map(|p| format!(" palette {p}")).unwrap_or_default();
        format!("{}{which} {}", self.character, self.element)
    }
}

/// Reads a `population-palette/set` payload.
pub fn parse_edit(payload: Option<&Value>) -> Result<Edit, String> {
    let text = |name: &str| payload.and_then(|p| p.get(name)).and_then(Value::as_str).map(str::to_string);
    let (Some(character), Some(element)) = (text("character"), text("element")) else {
        return Err(format!("a {SET_ACTION} action in mod.json needs \"character\" and \"element\""));
    };
    let palette = match payload.and_then(|p| p.get("palette")) {
        None | Some(Value::Null) => None,
        Some(v) => Some(v.as_u64().filter(|n| *n < 1000).ok_or(format!("\"palette\" of {SET_ACTION} must be a number"))? as u32),
    };
    let apply = match payload.and_then(|p| p.get("apply")) {
        None | Some(Value::Null) => None,
        Some(Value::Bool(b)) => Some(*b),
        Some(_) => return Err(format!("\"apply\" of {SET_ACTION} must be true or false")),
    };
    let colour = match text("colour") {
        None => None,
        Some(s) => Some(parse_hex(&s).ok_or(format!("\"colour\" of {SET_ACTION} must look like \"#ffffff\""))?),
    };
    if apply.is_none() && colour.is_none() {
        return Err(format!("a {SET_ACTION} action in mod.json needs \"apply\" or \"colour\""));
    }
    Ok(Edit { character, palette, element, apply, colour })
}

fn parse_hex(s: &str) -> Option<[u8; 3]> {
    let s = s.strip_prefix('#')?;
    if s.len() != 6 {
        return None;
    }
    let byte = |i: usize| u8::from_str_radix(s.get(i..i + 2)?, 16).ok();
    Some([byte(0)?, byte(2)?, byte(4)?])
}

struct Reader<'a> {
    d: &'a [u8],
    p: usize,
}

impl Reader<'_> {
    fn take(&mut self, n: usize) -> Result<&[u8], String> {
        let end = self.p.checked_add(n).filter(|e| *e <= self.d.len()).ok_or("the palette file ends early")?;
        let s = &self.d[self.p..end];
        self.p = end;
        Ok(s)
    }
    fn u8(&mut self) -> Result<u8, String> {
        Ok(self.take(1)?[0])
    }
    fn u32(&mut self) -> Result<u32, String> {
        let b = self.take(4)?;
        Ok(u32::from_le_bytes([b[0], b[1], b[2], b[3]]))
    }
    fn count(&mut self) -> Result<usize, String> {
        let n = self.u32()? as usize;
        if n > 100_000 {
            return Err("the palette file has an implausible list length".into());
        }
        Ok(n)
    }
    fn f32(&mut self) -> Result<f32, String> {
        Ok(f32::from_bits(self.u32()?))
    }
    fn string(&mut self) -> Result<String, String> {
        let n = self.count()?;
        let b = self.take(n)?;
        Ok(String::from_utf8_lossy(b.strip_suffix(&[0]).unwrap_or(b)).into_owned())
    }
}

fn find(d: &[u8], pat: &[u8], from: usize) -> Option<usize> {
    d.get(from..)?.windows(pat.len()).position(|w| w == pat).map(|i| i + from)
}

/// Every character's palettes, with where each colour and switch is stored.
pub fn parse(d: &[u8]) -> Result<Vec<Collection>, String> {
    let olst = find(d, b"OLST\0", 0).ok_or("not a palette file (no object list)")?;
    let mobj = find(d, b"MOBJ\0", olst).ok_or("not a palette file (no object)")?;
    let mut r = Reader { d, p: mobj + 5 };
    r.u32()?;
    let mut out = Vec::new();
    for _ in 0..r.count()? {
        let name = r.string()?;
        for _ in 0..r.count()? {
            r.u32()?;
            r.u32()?;
            if r.u8()? != 0 {
                r.take(16)?;
                r.string()?;
            }
            r.u32()?;
        }
        let mut palettes = Vec::new();
        for _ in 0..r.count()? {
            let mut elements = Vec::new();
            for _ in 0..r.count()? {
                let name = r.string()?;
                let colour_at = r.p;
                let colour = [r.f32()?, r.f32()?, r.f32()?];
                let apply_at = r.p;
                let apply = r.u8()? != 0;
                let layers = r.count()?;
                r.take(layers * 2)?;
                elements.push(Element { name, colour, apply, colour_at, apply_at });
            }
            palettes.push(elements);
        }
        out.push(Collection { name, palettes });
    }
    if r.p != d.len() {
        return Err("the palette file has data this version doesn't understand".into());
    }
    Ok(out)
}

/// The file with the edits written in. Edits for names or parts the file
/// doesn't have are returned as errors.
pub fn apply_edits(original: &[u8], edits: &[(String, Edit)]) -> Result<Vec<u8>, Vec<String>> {
    let collections = parse(original).map_err(|e| vec![e])?;
    let mut out = original.to_vec();
    let mut errors = Vec::new();
    for (owner, edit) in edits {
        let mut hit = false;
        for c in collections.iter().filter(|c| c.name.eq_ignore_ascii_case(&edit.character)) {
            for (i, palette) in c.palettes.iter().enumerate() {
                if edit.palette.is_some_and(|p| p as usize != i) {
                    continue;
                }
                for e in palette.iter().filter(|e| e.name.eq_ignore_ascii_case(&edit.element)) {
                    hit = true;
                    if let Some(apply) = edit.apply {
                        out[e.apply_at] = apply as u8;
                    }
                    if let Some(c) = edit.colour {
                        for (k, v) in c.iter().enumerate() {
                            out[e.colour_at + 4 * k..e.colour_at + 4 * k + 4].copy_from_slice(&(*v as f32 / 255.0).to_le_bytes());
                        }
                    }
                }
            }
        }
        if !hit {
            errors.push(format!("{owner} changes the background-character colour {}, which the game doesn't have.", edit.describe()));
        }
    }
    if errors.is_empty() {
        Ok(out)
    } else {
        Err(errors)
    }
}

/// The edits that turn `original` into `changed`, one per changed palette part.
pub fn diff(original: &[u8], changed: &[u8]) -> Result<Vec<Edit>, String> {
    let a = parse(original)?;
    let b = parse(changed).map_err(|e| format!("the dropped palette: {e}"))?;
    let shape = |c: &[Collection]| -> Vec<(String, Vec<Vec<String>>)> {
        c.iter().map(|c| (c.name.clone(), c.palettes.iter().map(|p| p.iter().map(|e| e.name.clone()).collect()).collect())).collect()
    };
    if shape(&a) != shape(&b) {
        return Err("the dropped palette lists different characters or parts than this game's, so its changes can't be worked out".into());
    }
    let byte = |f: f32| (f.clamp(0.0, 1.0) * 255.0).round() as u8;
    let mut edits = Vec::new();
    for (ca, cb) in a.iter().zip(&b) {
        let single = ca.palettes.len() == 1;
        for (i, (pa, pb)) in ca.palettes.iter().zip(&cb.palettes).enumerate() {
            for (ea, eb) in pa.iter().zip(pb) {
                let recoloured = ea.colour.iter().zip(&eb.colour).any(|(x, y)| byte(*x) != byte(*y));
                if !recoloured && ea.apply == eb.apply {
                    continue;
                }
                edits.push(Edit {
                    character: ca.name.clone(),
                    palette: (!single).then_some(i as u32),
                    element: ea.name.clone(),
                    apply: (ea.apply != eb.apply).then_some(eb.apply),
                    colour: recoloured.then(|| eb.colour.map(byte)),
                });
            }
        }
    }
    Ok(edits)
}

type Cached = (PathBuf, Vec<(String, u64, u128)>, Vec<u8>);
static ORIGINAL: Mutex<Option<Cached>> = Mutex::new(None);

/// The game's own palette file from its archives (cached while they don't change).
pub fn read_original(game_dir: &Path) -> Result<Vec<u8>, String> {
    let fingerprint = crate::assets::fingerprint(game_dir);
    let mut cache = ORIGINAL.lock().unwrap_or_else(|e| e.into_inner());
    if let Some((dir, print, bytes)) = cache.as_ref() {
        if dir == game_dir && *print == fingerprint {
            return Ok(bytes.clone());
        }
    }
    let wanted = GAME_PATH.to_ascii_lowercase();
    let codecs = crate::assets::GameCodecs::for_game(game_dir);
    for path in crate::assets::archive_files(game_dir) {
        let Ok(archive) = crate::dat::Archive::open(&path) else { continue };
        if let Some(entry) = archive.entries.iter().find(|e| e.path == wanted) {
            let mut file = File::open(&path).map_err(|e| format!("can't open {}: {e}", path.display()))?;
            let bytes = crate::dat::read_entry(&mut file, entry, &codecs)?;
            *cache = Some((game_dir.to_path_buf(), fingerprint, bytes.clone()));
            return Ok(bytes);
        }
    }
    Err(format!("the game's {GAME_PATH} isn't in the archives in {}", game_dir.display()))
}

/// The mod a palette file the player dropped becomes: its changes from the
/// game's file, as `population-palette/set` actions.
pub const CHANGES_ID: &str = "palette-changes";

pub fn is_palette_file(path: &Path) -> bool {
    path.file_name().is_some_and(|n| n.eq_ignore_ascii_case("characterpalettedata.prj"))
}

pub fn changes_manifest(edits: &[Edit], version: &str) -> String {
    let actions: Vec<Value> = edits
        .iter()
        .map(|e| serde_json::json!({ "typeId": SET_ACTION, "payload": e }))
        .collect();
    let manifest = serde_json::json!({
        "name": "My background-character colours",
        "version": version,
        "description": format!("{} colour changes taken from a CHARACTERPALETTEDATA.PRJ file.", edits.len()),
        "dependencies": [{ "id": PROVIDER, "minVersion": "1.0.0" }],
        "actions": actions,
    });
    serde_json::to_string_pretty(&manifest).unwrap_or_default()
}

pub fn write_changes_mod(edits: &[Edit], version: &str, out: &Path) -> Result<(), String> {
    use std::io::Write;
    let fail = |e: &dyn std::fmt::Display| format!("Can't write {}: {e}", out.display());
    let mut writer = zip::ZipWriter::new(File::create(out).map_err(|e| fail(&e))?);
    writer
        .start_file(format!("{CHANGES_ID}/mod.json"), zip::write::SimpleFileOptions::default())
        .map_err(|e| fail(&e))?;
    writer.write_all(changes_manifest(edits, version).as_bytes()).map_err(|e| fail(&e))?;
    writer.finish().map_err(|e| fail(&e))?;
    Ok(())
}

/// Which mods' edits clash, as (mod a, mod b, the part).
pub fn clashes(edits: &BTreeMap<String, Vec<Edit>>) -> Vec<(String, String, String)> {
    let mut out = Vec::new();
    let owners: Vec<(&String, &Vec<Edit>)> = edits.iter().collect();
    for (i, (a, ea)) in owners.iter().enumerate() {
        for (b, eb) in &owners[i + 1..] {
            if let Some(x) = ea.iter().find_map(|x| eb.iter().find(|y| x.clashes(y)).map(|_| x)) {
                out.push(((*a).clone(), (*b).clone(), x.describe()));
            }
        }
    }
    out
}

#[cfg(test)]
pub mod tests {
    use super::*;

    /// A palette file with the shape of the game's: two characters, the second with two palettes.
    pub fn sample() -> Vec<u8> {
        let mut d = b"....OLST\0....MOBJ\0".to_vec();
        let s = |d: &mut Vec<u8>, t: &str| {
            d.extend((t.len() as u32 + 1).to_le_bytes());
            d.extend(t.as_bytes());
            d.push(0);
        };
        d.extend(0u32.to_le_bytes());
        d.extend(2u32.to_le_bytes());
        for (name, palettes) in [("CloneTrooper", 1u32), ("City_Clone_Couruscant", 2)] {
            s(&mut d, name);
            d.extend(1u32.to_le_bytes());
            d.extend(7u32.to_le_bytes());
            d.extend(0u32.to_le_bytes());
            d.push(1);
            d.extend([9u8; 16]);
            s(&mut d, "chars/population/bulk/atli/popsystem_atlascity.texture");
            d.extend(3u32.to_le_bytes());
            d.extend(palettes.to_le_bytes());
            for p in 0..palettes {
                d.extend(2u32.to_le_bytes());
                for (el, c) in [("Upper Body", 1.0f32), ("Arms", 0.5 + p as f32 * 0.1)] {
                    s(&mut d, el);
                    for _ in 0..3 {
                        d.extend(c.to_le_bytes());
                    }
                    d.push(1);
                    d.extend(2u32.to_le_bytes());
                    d.extend([1, 0, 2, 0]);
                }
            }
        }
        d
    }

    fn edit(character: &str, palette: Option<u32>, element: &str, apply: Option<bool>, colour: Option<[u8; 3]>) -> Edit {
        Edit { character: character.into(), palette, element: element.into(), apply, colour }
    }

    #[test]
    fn reads_the_palettes_and_writes_edits_in_place() {
        let d = sample();
        let c = parse(&d).unwrap();
        assert_eq!(c.len(), 2);
        assert_eq!(c[1].name, "City_Clone_Couruscant");
        assert_eq!(c[1].palettes.len(), 2);
        assert_eq!(c[1].palettes[0][1].name, "Arms");
        let out = apply_edits(
            &d,
            &[("corusclone".into(), edit("city_clone_couruscant", None, "arms", Some(false), None)),
              ("x".into(), edit("CloneTrooper", None, "Upper Body", None, Some([255, 0, 0])))],
        )
        .unwrap();
        assert_eq!(out.len(), d.len());
        let c = parse(&out).unwrap();
        assert!(c[1].palettes.iter().all(|p| !p[1].apply));
        assert_eq!(c[0].palettes[0][0].colour, [1.0, 0.0, 0.0]);
        let err = apply_edits(&d, &[("m".into(), edit("Nobody", None, "Arms", Some(false), None))]).unwrap_err();
        assert!(err[0].contains("m changes"));
    }

    #[test]
    fn works_out_the_changes_in_an_edited_file() {
        let d = sample();
        let changed = apply_edits(
            &d,
            &[("a".into(), edit("City_Clone_Couruscant", Some(1), "Arms", None, Some([10, 20, 30]))),
              ("b".into(), edit("CloneTrooper", None, "Arms", Some(false), None))],
        )
        .unwrap();
        let edits = diff(&d, &changed).unwrap();
        assert_eq!(
            edits,
            vec![edit("CloneTrooper", None, "Arms", Some(false), None), edit("City_Clone_Couruscant", Some(1), "Arms", None, Some([10, 20, 30]))]
        );
        assert_eq!(apply_edits(&d, &edits.into_iter().map(|e| ("m".to_string(), e)).collect::<Vec<_>>()).unwrap(), changed);
    }

    #[test]
    fn finds_clashing_changes_only() {
        let mut m = BTreeMap::new();
        m.insert("a".to_string(), vec![edit("C", None, "Arms", Some(false), None)]);
        m.insert("b".to_string(), vec![edit("c", Some(0), "arms", None, Some([1, 2, 3]))]);
        assert!(clashes(&m).is_empty());
        m.insert("c".to_string(), vec![edit("C", Some(0), "Arms", Some(true), None)]);
        let found = clashes(&m);
        assert_eq!(found.len(), 1);
        assert_eq!((found[0].0.as_str(), found[0].1.as_str()), ("a", "c"));
    }

    #[test]
    fn a_changes_manifest_reads_back_as_the_same_edits() {
        let edits = vec![edit("City_Clone_Couruscant", Some(2), "Arms", Some(true), Some([1, 2, 3]))];
        let m = crate::package::parse_manifest(&changes_manifest(&edits, "1.0.1")).unwrap();
        assert_eq!(m.palette_edits, edits);
        assert_eq!(m.dependencies[0].id, PROVIDER);
    }

    #[test]
    fn reads_payloads() {
        let v: Value = serde_json::json!({"character":"X","element":"Arms","apply":false,"colour":"#FF0010"});
        assert_eq!(parse_edit(Some(&v)).unwrap(), edit("X", None, "Arms", Some(false), Some([255, 0, 16])));
        assert!(parse_edit(Some(&serde_json::json!({"character":"X","element":"Arms"}))).is_err());
        assert!(parse_edit(Some(&serde_json::json!({"character":"X","element":"Arms","colour":"red"}))).is_err());
    }
}
