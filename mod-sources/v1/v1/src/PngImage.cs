using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Minimal PNG reader (no external dependencies). Supports all colour types,
    // bit depths 1-16, non-interlaced images. Output: RGBA8, top-down.
    static class PngImage
    {
        public static (int Width, int Height, byte[] Rgba) Load(string path)
        {
            var data = File.ReadAllBytes(path);
            ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(signature)) throw new InvalidDataException($"{path} is not a PNG file");

            int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
            byte[]? palette = null, transparency = null;
            using var idat = new MemoryStream();

            var o = 8;
            while (o + 8 <= data.Length)
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(o));
                var type = System.Text.Encoding.ASCII.GetString(data, o + 4, 4);
                var body = data.AsSpan(o + 8, length);
                switch (type)
                {
                    case "IHDR":
                        width = (int)BinaryPrimitives.ReadUInt32BigEndian(body);
                        height = (int)BinaryPrimitives.ReadUInt32BigEndian(body[4..]);
                        bitDepth = body[8];
                        colorType = body[9];
                        interlace = body[12];
                        break;
                    case "PLTE": palette = body.ToArray(); break;
                    case "tRNS": transparency = body.ToArray(); break;
                    case "IDAT": idat.Write(body); break;
                }
                if (type == "IEND") break;
                o += 12 + length;
            }

            if (interlace != 0) throw new InvalidDataException($"{path}: interlaced PNGs are not supported, save it without interlacing");

            int channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new InvalidDataException($"{path}: unsupported PNG colour type {colorType}") };
            var bitsPerPixel = channels * bitDepth;
            var bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
            var stride = (width * bitsPerPixel + 7) / 8;

            idat.Position = 0;
            var raw = new byte[(stride + 1) * height];
            using (var z = new ZLibStream(idat, CompressionMode.Decompress))
            {
                var read = 0;
                while (read < raw.Length)
                {
                    var n = z.Read(raw, read, raw.Length - read);
                    if (n == 0) throw new InvalidDataException($"{path}: truncated image data");
                    read += n;
                }
            }

            // Undo scanline filters
            var pixels = new byte[stride * height];
            for (var y = 0; y < height; y++)
            {
                var filter = raw[y * (stride + 1)];
                var src = raw.AsSpan(y * (stride + 1) + 1, stride);
                var dst = pixels.AsSpan(y * stride, stride);
                var prev = y > 0 ? pixels.AsSpan((y - 1) * stride, stride) : Span<byte>.Empty;
                for (var x = 0; x < stride; x++)
                {
                    int a = x >= bytesPerPixel ? dst[x - bytesPerPixel] : 0;
                    int b = y > 0 ? prev[x] : 0;
                    int c = x >= bytesPerPixel && y > 0 ? prev[x - bytesPerPixel] : 0;
                    dst[x] = (byte)(filter switch
                    {
                        0 => src[x],
                        1 => src[x] + a,
                        2 => src[x] + b,
                        3 => src[x] + (a + b) / 2,
                        4 => src[x] + Paeth(a, b, c),
                        _ => throw new InvalidDataException($"{path}: bad PNG filter {filter}"),
                    });
                }
            }

            var rgba = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    int Sample(int channel)
                    {
                        if (bitDepth == 8) return pixels[y * stride + x * channels + channel];
                        if (bitDepth == 16) return pixels[y * stride + (x * channels + channel) * 2];
                        var bitIndex = (x * channels + channel) * bitDepth;
                        var v = (pixels[y * stride + bitIndex / 8] >> (8 - bitDepth - bitIndex % 8)) & ((1 << bitDepth) - 1);
                        return colorType == 3 ? v : v * 255 / ((1 << bitDepth) - 1);
                    }

                    var o4 = (y * width + x) * 4;
                    switch (colorType)
                    {
                        case 0:
                            rgba[o4] = rgba[o4 + 1] = rgba[o4 + 2] = (byte)Sample(0);
                            rgba[o4 + 3] = 255;
                            break;
                        case 2:
                            rgba[o4] = (byte)Sample(0);
                            rgba[o4 + 1] = (byte)Sample(1);
                            rgba[o4 + 2] = (byte)Sample(2);
                            rgba[o4 + 3] = 255;
                            break;
                        case 3:
                            var i = Sample(0);
                            if (palette == null || i * 3 + 2 >= palette.Length) throw new InvalidDataException($"{path}: bad palette");
                            rgba[o4] = palette[i * 3];
                            rgba[o4 + 1] = palette[i * 3 + 1];
                            rgba[o4 + 2] = palette[i * 3 + 2];
                            rgba[o4 + 3] = transparency != null && i < transparency.Length ? transparency[i] : (byte)255;
                            break;
                        case 4:
                            rgba[o4] = rgba[o4 + 1] = rgba[o4 + 2] = (byte)Sample(0);
                            rgba[o4 + 3] = (byte)Sample(1);
                            break;
                        case 6:
                            rgba[o4] = (byte)Sample(0);
                            rgba[o4 + 1] = (byte)Sample(1);
                            rgba[o4 + 2] = (byte)Sample(2);
                            rgba[o4 + 3] = (byte)Sample(3);
                            break;
                    }
                }
            }

            return (width, height, rgba);
        }

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        // Fit the image inside size x size (keeping aspect ratio, centred, transparent padding).
        // Area-averaged with premultiplied alpha so edges don't get dark halos.
        public static byte[] FitSquare(int width, int height, byte[] rgba, int size)
        {
            var output = new byte[size * size * 4];
            var scale = Math.Min((double)size / width, (double)size / height);
            var outW = Math.Max(1, (int)Math.Round(width * scale));
            var outH = Math.Max(1, (int)Math.Round(height * scale));
            var offX = (size - outW) / 2;
            var offY = (size - outH) / 2;

            for (var oy = 0; oy < outH; oy++)
            {
                double sy0 = oy / scale, sy1 = Math.Min(height, (oy + 1) / scale);
                for (var ox = 0; ox < outW; ox++)
                {
                    double sx0 = ox / scale, sx1 = Math.Min(width, (ox + 1) / scale);
                    double r = 0, g = 0, b = 0, a = 0, total = 0;
                    for (var sy = (int)sy0; sy < Math.Max((int)sy0 + 1, (int)Math.Ceiling(sy1)); sy++)
                    {
                        var wy = Math.Min(sy + 1, sy1) - Math.Max(sy, sy0);
                        if (wy <= 0) wy = 1;
                        for (var sx = (int)sx0; sx < Math.Max((int)sx0 + 1, (int)Math.Ceiling(sx1)); sx++)
                        {
                            var wx = Math.Min(sx + 1, sx1) - Math.Max(sx, sx0);
                            if (wx <= 0) wx = 1;
                            var weight = wx * wy;
                            var i = (Math.Min(sy, height - 1) * width + Math.Min(sx, width - 1)) * 4;
                            var alpha = rgba[i + 3] / 255.0;
                            r += rgba[i] * alpha * weight;
                            g += rgba[i + 1] * alpha * weight;
                            b += rgba[i + 2] * alpha * weight;
                            a += alpha * weight;
                            total += weight;
                        }
                    }
                    var o = ((oy + offY) * size + ox + offX) * 4;
                    if (a > 0)
                    {
                        output[o] = (byte)Math.Clamp(Math.Round(r / a), 0, 255);
                        output[o + 1] = (byte)Math.Clamp(Math.Round(g / a), 0, 255);
                        output[o + 2] = (byte)Math.Clamp(Math.Round(b / a), 0, 255);
                    }
                    output[o + 3] = (byte)Math.Clamp(Math.Round(a / total * 255), 0, 255);
                }
            }
            return output;
        }
    }
}
