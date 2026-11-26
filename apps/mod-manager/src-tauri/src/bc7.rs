//! BC7 compression of the character maker's textures (Intel's ISPC texture
//! compressor): every mip level down to 1x1, as the game's .texture files hold them.

/// Colours behind full transparency are invisible but still steer block
/// compression: give each fully transparent pixel the alpha-weighted mean
/// colour of its 4x4 block (black when nothing in the block is visible).
pub fn bleed_hidden(px: &mut [u8], w: usize, h: usize) {
    for by in (0..h).step_by(4) {
        for bx in (0..w).step_by(4) {
            let (mut r, mut g, mut b, mut n) = (0u64, 0u64, 0u64, 0u64);
            for y in by..(by + 4).min(h) {
                for x in bx..(bx + 4).min(w) {
                    let i = (y * w + x) * 4;
                    let a = px[i + 3] as u64;
                    r += px[i] as u64 * a;
                    g += px[i + 1] as u64 * a;
                    b += px[i + 2] as u64 * a;
                    n += a;
                }
            }
            let c = if n > 0 { [(r / n) as u8, (g / n) as u8, (b / n) as u8] } else { [0, 0, 0] };
            for y in by..(by + 4).min(h) {
                for x in bx..(bx + 4).min(w) {
                    let i = (y * w + x) * 4;
                    if px[i + 3] == 0 {
                        px[i..i + 3].copy_from_slice(&c);
                    }
                }
            }
        }
    }
}

/// Half size, averaging 2x2 pixels (edge pixels repeat on odd sizes).
fn halve(px: &[u8], w: usize, h: usize) -> (Vec<u8>, usize, usize) {
    let (nw, nh) = ((w / 2).max(1), (h / 2).max(1));
    let mut out = vec![0u8; nw * nh * 4];
    for y in 0..nh {
        for x in 0..nw {
            for k in 0..4 {
                let mut s = 0u32;
                for (dx, dy) in [(0, 0), (1, 0), (0, 1), (1, 1)] {
                    let (sx, sy) = ((2 * x + dx).min(w - 1), (2 * y + dy).min(h - 1));
                    s += px[(sy * w + sx) * 4 + k] as u32;
                }
                out[(y * nw + x) * 4 + k] = ((s + 2) / 4) as u8;
            }
        }
    }
    (out, nw, nh)
}

/// One level, padded to whole 4x4 blocks by repeating the edge pixels.
fn compress_level(px: &[u8], w: usize, h: usize) -> Vec<u8> {
    let (pw, ph) = (w.div_ceil(4) * 4, h.div_ceil(4) * 4);
    let mut padded = vec![0u8; pw * ph * 4];
    for y in 0..ph {
        for x in 0..pw {
            let s = (y.min(h - 1) * w + x.min(w - 1)) * 4;
            padded[(y * pw + x) * 4..(y * pw + x) * 4 + 4].copy_from_slice(&px[s..s + 4]);
        }
    }
    let surface = intel_tex_2::RgbaSurface { data: &padded, width: pw as u32, height: ph as u32, stride: pw as u32 * 4 };
    intel_tex_2::bc7::compress_blocks(&intel_tex_2::bc7::alpha_fast_settings(), &surface)
}

/// All mip levels of an RGBA image, compressed: (level count, data).
pub fn encode_mips(rgba: &[u8], w: usize, h: usize) -> Result<(u32, Vec<u8>), String> {
    if w == 0 || h == 0 || rgba.len() != w * h * 4 {
        return Err(format!("expected {w}x{h} RGBA pixels ({} bytes), got {} bytes", w * h * 4, rgba.len()));
    }
    let (mut cur, mut cw, mut ch) = (rgba.to_vec(), w, h);
    let (mut levels, mut out) = (0u32, Vec::new());
    loop {
        bleed_hidden(&mut cur, cw, ch);
        out.extend(compress_level(&cur, cw, ch));
        levels += 1;
        if cw == 1 && ch == 1 {
            break;
        }
        (cur, cw, ch) = halve(&cur, cw, ch);
    }
    Ok((levels, out))
}

#[cfg(test)]
mod tests {
    use super::*;

    /// The size the game expects: 16 bytes per 4x4 block on every level.
    fn bc7_size(w: usize, h: usize, levels: u32) -> usize {
        (0..levels).map(|i| (w >> i).max(1).div_ceil(4) * (h >> i).max(1).div_ceil(4) * 16).sum()
    }

    #[test]
    fn encodes_every_level_at_the_size_the_game_expects() {
        let (w, h) = (64, 32);
        let mut px = vec![0u8; w * h * 4];
        for y in 0..h {
            for x in 0..w {
                px[(y * w + x) * 4..(y * w + x) * 4 + 4].copy_from_slice(&[(x * 4) as u8, (y * 8) as u8, 128, if x < 20 { 0 } else { 255 }]);
            }
        }
        let (levels, data) = encode_mips(&px, w, h).unwrap();
        assert_eq!(levels, 7); // 64x32, 32x16, 16x8, 8x4, 4x2, 2x1, 1x1
        assert_eq!(data.len(), bc7_size(w, h, levels));
        assert!(data.chunks(16).all(|b| b.iter().any(|&x| x != 0)), "every block holds a BC7 mode");
        assert!(encode_mips(&px[4..], w, h).is_err());
    }

    #[test]
    fn hidden_pixels_take_the_visible_colour_of_their_block() {
        // a 4x4 block: one visible orange pixel, the rest transparent magenta
        let mut px = [200u8, 40, 180, 0].repeat(16);
        px[0..4].copy_from_slice(&[240, 128, 40, 255]);
        bleed_hidden(&mut px, 4, 4);
        assert_eq!(&px[4..8], &[240, 128, 40, 0]);
        assert_eq!(&px[0..4], &[240, 128, 40, 255]);
        // nothing visible: black
        let mut px = [200u8, 40, 180, 0].repeat(16);
        bleed_hidden(&mut px, 4, 4);
        assert!(px.chunks(4).all(|p| p == [0, 0, 0, 0]));
    }
}
