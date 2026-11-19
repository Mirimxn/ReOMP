//! Reads the game's PC `.DAT` archives in place: the index at the end of the
//! file, the name tree, and each entry's chunks. Ported from the minifig
//! viewer's `tools/minifig-viewer/gamedat.js` (itself a port of DATExtract's
//! new-format reader) and checked against it on real archives.
//! See plans/mod-manager.md#game-assets-and-replacement-mods.

use std::collections::HashMap;
use std::fs::File;
use std::io::{self, Read, Seek, SeekFrom};
use std::path::{Path, PathBuf};

/// Indexes larger than this are treated as damaged.
const MAX_INDEX: u64 = 512 * 1024 * 1024;
/// Entries larger than this aren't read into memory.
pub const MAX_ENTRY: u64 = 2 * 1024 * 1024 * 1024;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Entry {
    /// Lower-case logical path with `/`, as the game looks it up.
    pub path: String,
    pub offset: u64,
    pub stored_size: u64,
    pub size: u64,
    pub packed: bool,
}

#[derive(Debug, Clone)]
pub struct Archive {
    pub file: PathBuf,
    pub entries: Vec<Entry>,
}

fn be32(b: &[u8], at: usize) -> Result<u32, String> {
    b.get(at..at + 4)
        .map(|s| u32::from_be_bytes([s[0], s[1], s[2], s[3]]))
        .ok_or_else(|| "the archive index is cut short".to_string())
}

fn be16(b: &[u8], at: usize) -> Result<u16, String> {
    b.get(at..at + 2)
        .map(|s| u16::from_be_bytes([s[0], s[1]]))
        .ok_or_else(|| "the archive index is cut short".to_string())
}

fn read_at(file: &mut File, offset: u64, len: usize) -> io::Result<Vec<u8>> {
    file.seek(SeekFrom::Start(offset))?;
    let mut buf = vec![0u8; len];
    file.read_exact(&mut buf)?;
    Ok(buf)
}

/// FNV-1a 64 of the path with ASCII letters upper-cased, as the game hashes it.
pub fn fnv64(path: &str) -> u64 {
    let mut h: u64 = 0xcbf2_9ce4_8422_2325;
    for c in path.bytes() {
        h ^= c.to_ascii_uppercase() as u64;
        h = h.wrapping_mul(0x0000_0100_0000_01b3);
    }
    h
}

/// The 32-bit variant older archives use.
pub fn fnv32(path: &str) -> u32 {
    let mut h: u32 = 0x811c_9dc5;
    for c in path.bytes() {
        h = (h ^ c.to_ascii_uppercase() as u32).wrapping_mul(0x0019_9933);
    }
    h
}

impl Archive {
    /// Reads only the index; entry data is read on demand.
    pub fn open(path: &Path) -> Result<Archive, String> {
        let mut file = File::open(path).map_err(|e| format!("can't open it: {e}"))?;
        let length = file.metadata().map_err(|e| e.to_string())?.len();
        let head = read_at(&mut file, 0, 8).map_err(|_| "it's too short to be an archive".to_string())?;
        let locator = u32::from_le_bytes([head[0], head[1], head[2], head[3]]) as u64;
        let index_len = u32::from_le_bytes([head[4], head[5], head[6], head[7]]) as u64;
        let mut offset = if locator & 0x8000_0000 != 0 { ((locator ^ 0xffff_ffff) + 1) * 256 } else { locator };
        if length > 0xffff_ffff && offset < 0x1_0000_0000 {
            offset += 0x1_0000_0000;
        }
        if index_len < 32 || index_len > MAX_INDEX || offset.checked_add(index_len).is_none_or(|end| end > length) {
            return Err("its index is outside the file".into());
        }
        let h = read_at(&mut file, offset, index_len as usize).map_err(|e| format!("can't read its index: {e}"))?;
        if &h[4..8] != b".CC4" {
            return Err("it isn't a supported archive (no .CC4 marker)".into());
        }
        let version = be32(&h, 12)? as i32;
        let node_count = be32(&h, 24)? as usize;
        let names_len = be32(&h, 28)? as usize;

        // Name tree: folders are remembered by node index, files collected in order.
        let mut p = 32 + 4 + names_len;
        let mut folders: HashMap<usize, String> = HashMap::new();
        let mut paths: Vec<String> = Vec::new();
        for i in 0..node_count {
            let name_offset = be32(&h, p)?;
            let folder = be16(&h, p + 4)? as usize;
            let mut file_id = be16(&h, p + 10)? as usize;
            p += 12;
            if name_offset == 0xffff_ffff {
                continue;
            }
            let start = 32usize.checked_add(name_offset as usize).ok_or("bad name offset")?;
            let end = h.get(start..).and_then(|s| s.iter().position(|&b| b == 0)).map(|n| start + n);
            let Some(end) = end else { return Err("a name in the index isn't terminated".into()) };
            let name: String = h[start..end].iter().map(|&b| b as char).collect();
            if i == node_count - 1 {
                file_id = paths.len();
            }
            let full = format!("{}\\{}", folders.get(&folder).map_or("", String::as_str), name);
            if file_id != 0 {
                paths.push(full);
            } else {
                folders.insert(i, full);
            }
        }

        p += 4;
        let file_count = be32(&h, p)? as usize;
        p += 4;
        if file_count.checked_mul(16).is_none_or(|n| p + n > h.len()) {
            return Err("its file table is cut short".into());
        }
        let mut entries = Vec::with_capacity(file_count);
        for _ in 0..file_count {
            let hi = be32(&h, p)? as u64;
            let lo = be32(&h, p + 4)? as u64;
            let stored_size = be32(&h, p + 8)? as u64;
            let mut size = be32(&h, p + 12)? as u64;
            let (offset, packed) = if version <= -13 {
                (((hi & 0x00ff_ffff) << 32) | lo, hi >> 24 != 0)
            } else if version <= -10 {
                let packed = size >> 31 != 0;
                size &= 0x7fff_ffff;
                ((hi << 32) | lo, packed)
            } else {
                ((hi << 32) | lo, false)
            };
            let in_bounds = offset.checked_add(stored_size).is_some_and(|end| end <= length);
            entries.push(Entry { path: String::new(), offset: if in_bounds { offset } else { u64::MAX }, stored_size, size, packed });
            p += 16;
        }

        // Names reach entries through a table of path hashes after the file table.
        let mut by64: HashMap<u64, usize> = HashMap::new();
        let mut by32: HashMap<u32, usize> = HashMap::new();
        for i in 0..file_count {
            if let (Ok(a), Ok(b)) = (be32(&h, p + 8 * i), be32(&h, p + 8 * i + 4)) {
                by64.insert(((a as u64) << 32) | b as u64, i);
            }
            if let Ok(a) = be32(&h, p + 4 * i) {
                by32.insert(a, i);
            }
        }
        let sample = &paths[..paths.len().min(200)];
        let hits64 = sample.iter().filter(|n| by64.contains_key(&fnv64(&n[1..]))).count();
        let hits32 = sample.iter().filter(|n| by32.contains_key(&fnv32(&n[1..]))).count();
        for name in &paths {
            let key = &name[1..];
            let index = if hits64 >= hits32 { by64.get(&fnv64(key)) } else { by32.get(&fnv32(key)) };
            if let Some(&i) = index {
                entries[i].path = key.replace('\\', "/").to_lowercase();
            }
        }
        entries.retain(|e| !e.path.is_empty() && e.offset != u64::MAX);
        Ok(Archive { file: path.to_path_buf(), entries })
    }
}

/// Decodes one chunk type. `Oodle` is provided by the platform.
pub trait Codecs {
    fn oodle(&self, input: &[u8], size: usize) -> Result<Vec<u8>, String>;
}

/// The compression an entry uses, for showing when it can't be extracted.
pub fn codec_of(file: &mut File, entry: &Entry) -> Result<String, String> {
    if !entry.packed && entry.stored_size == entry.size {
        return Ok("raw".into());
    }
    let head = read_at(file, entry.offset, 12.min(entry.stored_size as usize)).map_err(|e| e.to_string())?;
    Ok(head.get(..4).map(|t| t.iter().map(|&b| b as char).collect()).unwrap_or_default())
}

/// Reads and decodes one entry, checking every size along the way.
pub fn read_entry(file: &mut File, entry: &Entry, codecs: &dyn Codecs) -> Result<Vec<u8>, String> {
    if entry.size > MAX_ENTRY || entry.stored_size > MAX_ENTRY {
        return Err("the file is too large to extract".into());
    }
    let stored = read_at(file, entry.offset, entry.stored_size as usize).map_err(|e| format!("can't read it: {e}"))?;
    if !entry.packed && entry.stored_size == entry.size {
        return Ok(stored);
    }
    let size = entry.size as usize;
    let mut out = Vec::with_capacity(size);
    let mut o = 0usize;
    while o < stored.len() && out.len() < size {
        let header = stored.get(o..o + 12).ok_or("a chunk header is cut short")?;
        let tag: String = header[..4].iter().map(|&b| b as char).collect();
        let packed_len = i32::from_le_bytes([header[4], header[5], header[6], header[7]]);
        let plain_len = i32::from_le_bytes([header[8], header[9], header[10], header[11]]);
        if packed_len < 0 || plain_len < 0 || out.len() + plain_len as usize > size {
            return Err(format!("a {tag} chunk has impossible sizes"));
        }
        let data = stored.get(o + 12..o + 12 + packed_len as usize).ok_or("a chunk runs past the entry")?;
        let decoded = if packed_len == plain_len {
            data.to_vec()
        } else {
            match tag.as_str() {
                "DFLT" => inflate_tt(data, plain_len as usize)?,
                "OODL" => codecs.oodle(data, plain_len as usize)?,
                other => return Err(format!("it uses {other} compression, which can't be extracted yet")),
            }
        };
        if decoded.len() != plain_len as usize {
            return Err(format!("a {tag} chunk unpacked to {} of {plain_len} bytes", decoded.len()));
        }
        out.extend_from_slice(&decoded);
        o += 12 + packed_len as usize;
    }
    if out.len() != size {
        return Err(format!("it unpacked to {} of {size} bytes", out.len()));
    }
    Ok(out)
}

// ---------------------------------------------------------------- DFLT

/// Raw deflate with TT's block types: 0 = dynamic Huffman, 1 = fixed, 2 = stored.
pub fn inflate_tt(src: &[u8], size: usize) -> Result<Vec<u8>, String> {
    const LBASE: [u16; 29] = [3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258];
    const LEXT: [u8; 29] = [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0];
    const DBASE: [u16; 30] = [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577];
    const DEXT: [u8; 30] = [0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13];
    const ORDER: [usize; 19] = [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    struct Bits<'a> {
        src: &'a [u8],
        pos: usize,
        buf: u64,
        count: u32,
    }
    impl Bits<'_> {
        fn need(&mut self, n: u32) {
            while self.count < n {
                let byte = self.src.get(self.pos).copied().unwrap_or(0);
                self.pos += 1;
                self.buf |= (byte as u64) << self.count;
                self.count += 8;
            }
        }
        fn take(&mut self, n: u32) -> Result<u32, String> {
            if n == 0 {
                return Ok(0);
            }
            self.need(n);
            if self.pos > self.src.len() + 8 {
                return Err("DFLT data ends early".into());
            }
            let v = (self.buf & ((1u64 << n) - 1)) as u32;
            self.buf >>= n;
            self.count -= n;
            Ok(v)
        }
    }
    struct Table {
        entries: Vec<i32>,
        max: u32,
    }
    fn table(lengths: &[u8]) -> Table {
        let max = lengths.iter().copied().max().unwrap_or(0).max(1) as u32;
        let mut count = [0u16; 16];
        for &l in lengths {
            count[l as usize] += 1;
        }
        count[0] = 0;
        let mut next = [0u16; 16];
        let mut code = 0u16;
        for bits in 1..16 {
            code = (code + count[bits - 1]) << 1;
            next[bits] = code;
        }
        let mut entries = vec![-1i32; 1 << max];
        for (symbol, &l) in lengths.iter().enumerate() {
            if l == 0 {
                continue;
            }
            let mut c = next[l as usize];
            next[l as usize] += 1;
            let mut reversed = 0usize;
            for _ in 0..l {
                reversed = (reversed << 1) | (c & 1) as usize;
                c >>= 1;
            }
            let mut k = reversed;
            while k < (1 << max) {
                entries[k] = ((symbol as i32) << 4) | l as i32;
                k += 1 << l;
            }
        }
        Table { entries, max }
    }
    fn decode(bits: &mut Bits, t: &Table) -> Result<usize, String> {
        bits.need(t.max);
        let e = t.entries[(bits.buf & ((1u64 << t.max) - 1)) as usize];
        if e < 0 {
            return Err("DFLT data has an invalid code".into());
        }
        let l = (e & 15) as u32;
        bits.buf >>= l;
        bits.count -= l;
        Ok((e >> 4) as usize)
    }

    let mut out: Vec<u8> = Vec::with_capacity(size);
    let mut bits = Bits { src, pos: 0, buf: 0, count: 0 };
    let mut fixed: Option<(Table, Table)> = None;
    loop {
        let last = bits.take(1)?;
        let kind = bits.take(2)?;
        if kind == 2 {
            let drop = bits.count % 8;
            bits.take(drop)?;
            let len = bits.take(16)?;
            bits.take(16)?;
            for _ in 0..len {
                out.push(bits.take(8)? as u8);
            }
        } else {
            let dynamic;
            let (lit, dist) = if kind == 1 {
                let tables = fixed.get_or_insert_with(|| {
                    let mut l = [0u8; 288];
                    for (i, v) in l.iter_mut().enumerate() {
                        *v = if i < 144 { 8 } else if i < 256 { 9 } else if i < 280 { 7 } else { 8 };
                    }
                    (table(&l), table(&[5u8; 30]))
                });
                (&tables.0, &tables.1)
            } else if kind == 0 {
                let hlit = bits.take(5)? as usize + 257;
                let hdist = bits.take(5)? as usize + 1;
                let hclen = bits.take(4)? as usize + 4;
                let mut cl = [0u8; 19];
                for &slot in ORDER.iter().take(hclen) {
                    cl[slot] = bits.take(3)? as u8;
                }
                let ct = table(&cl);
                let mut lengths: Vec<u8> = Vec::with_capacity(hlit + hdist);
                while lengths.len() < hlit + hdist {
                    let s = decode(&mut bits, &ct)?;
                    match s {
                        0..=15 => lengths.push(s as u8),
                        16 => {
                            let prev = *lengths.last().ok_or("DFLT repeats before any length")?;
                            for _ in 0..3 + bits.take(2)? {
                                lengths.push(prev);
                            }
                        }
                        17 => lengths.extend(std::iter::repeat_n(0, 3 + bits.take(3)? as usize)),
                        _ => lengths.extend(std::iter::repeat_n(0, 11 + bits.take(7)? as usize)),
                    }
                }
                if lengths.len() > hlit + hdist {
                    return Err("DFLT code lengths overflow".into());
                }
                dynamic = (table(&lengths[..hlit]), table(&lengths[hlit..]));
                (&dynamic.0, &dynamic.1)
            } else {
                return Err("DFLT data has an invalid block type".into());
            };
            loop {
                let s = decode(&mut bits, lit)?;
                if s < 256 {
                    out.push(s as u8);
                } else if s == 256 {
                    break;
                } else {
                    let k = s - 257;
                    if k >= 29 {
                        return Err("DFLT data has an invalid length".into());
                    }
                    let len = LBASE[k] as usize + bits.take(LEXT[k] as u32)? as usize;
                    let d = decode(&mut bits, dist)?;
                    if d >= 30 {
                        return Err("DFLT data has an invalid distance".into());
                    }
                    let back = DBASE[d] as usize + bits.take(DEXT[d] as u32)? as usize;
                    if back > out.len() {
                        return Err("DFLT data points before the start".into());
                    }
                    let from = out.len() - back;
                    for i in 0..len {
                        let b = out[from + i];
                        out.push(b);
                    }
                }
                if out.len() > size {
                    return Err("DFLT data unpacks past its size".into());
                }
            }
        }
        if out.len() > size {
            return Err("DFLT data unpacks past its size".into());
        }
        if last == 1 {
            break;
        }
    }
    Ok(out)
}
