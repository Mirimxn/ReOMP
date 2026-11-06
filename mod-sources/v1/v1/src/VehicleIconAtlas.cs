using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Reads/writes the game's icon sheets (vehicles 256 px cells, characters 128 px cells), e.g.:
    //   UI/ICONS/VEHICLES/VEHICLES.TSH          sprite index (name -> rectangle)
    //   UI/ICONS/VEHICLES/VEHICLES_DX11.TEXTURE one DXT5 (BC3) image, 1 mip, 256x256 cells
    // Rectangles are stored as pixel x/y/w/h (y = memory row) plus UVs (v flipped).
    // New icons go into free cells; when none are left the sheet grows by whole rows
    // of cells at the end of the image, so existing icons keep their pixel positions.
    static class VehicleIconAtlas
    {
        // Cell size of the sheet being built (256 for vehicles, 128 for characters).
        [ThreadStatic] public static int Cell;

        public sealed class Sprite
        {
            public required string Name;
            public uint X, Y, W, H;
            public byte[]? OriginalUv; // kept when the sheet size does not change
        }

        public sealed class SpriteIndex
        {
            readonly byte[] _head;
            readonly byte[] _tail;
            readonly int _chunk2SizeOffset;
            public readonly List<Sprite> Sprites = [];

            public SpriteIndex(byte[] data)
            {
                var chunk1Size = (int)ReadU32(data, 0);
                _chunk2SizeOffset = 4 + chunk1Size;
                var chunk2Size = (int)ReadU32(data, _chunk2SizeOffset);
                if (_chunk2SizeOffset + 4 + chunk2Size != data.Length) throw new InvalidDataException("TSH: unexpected chunk sizes");

                var hsxt = IndexOf(data, "HSXT"u8.ToArray(), _chunk2SizeOffset);
                if (hsxt < 0) throw new InvalidDataException("TSH: HSXT block not found");
                var countOffset = hsxt + 4 + 8;
                var count = (int)ReadU32(data, countOffset);

                var o = countOffset + 4;
                for (var i = 0; i < count; i++)
                {
                    var uv = data.AsSpan(o, 16).ToArray();
                    var x = ReadU32(data, o + 16);
                    var y = ReadU32(data, o + 20);
                    var w = ReadU32(data, o + 24);
                    var h = ReadU32(data, o + 28);
                    var nameLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(o + 32));
                    var name = Encoding.UTF8.GetString(data, o + 34, nameLength - 1);
                    Sprites.Add(new Sprite { Name = name, X = x, Y = y, W = w, H = h, OriginalUv = uv });
                    o += 34 + nameLength + 8;
                }

                _head = data[..countOffset];
                _tail = data[o..];
            }

            public byte[] Build(int texWidth, int texHeight, bool keepOriginalUvs)
            {
                using var ms = new MemoryStream();
                ms.Write(_head);
                WriteU32(ms, (uint)Sprites.Count);
                foreach (var s in Sprites)
                {
                    if (keepOriginalUvs && s.OriginalUv != null)
                    {
                        ms.Write(s.OriginalUv);
                    }
                    else
                    {
                        WriteF32(ms, (float)s.X / texWidth);
                        WriteF32(ms, 1f - (float)(s.Y + s.H) / texHeight);
                        WriteF32(ms, (float)(s.X + s.W) / texWidth);
                        WriteF32(ms, 1f - (float)s.Y / texHeight);
                    }
                    WriteU32(ms, s.X);
                    WriteU32(ms, s.Y);
                    WriteU32(ms, s.W);
                    WriteU32(ms, s.H);
                    var nameBytes = Encoding.UTF8.GetBytes(s.Name);
                    var lengthBytes = new byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(lengthBytes, (ushort)(nameBytes.Length + 1));
                    ms.Write(lengthBytes);
                    ms.Write(nameBytes);
                    ms.WriteByte(0);
                    ms.Write(SpriteHash(s.Name));
                }
                ms.Write(_tail);

                var result = ms.ToArray();
                BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(_chunk2SizeOffset), (uint)(result.Length - _chunk2SizeOffset - 4));
                return result;
            }
        }

        public sealed class Sheet
        {
            readonly byte[] _head;
            readonly int _chunk2SizeOffset;
            readonly int _heightOffset;
            public readonly int Width;
            public int Height { get; private set; }
            byte[] _pixels;

            public Sheet(byte[] data)
            {
                var chunk1Size = (int)ReadU32(data, 0);
                _chunk2SizeOffset = 4 + chunk1Size;
                var rtxt = IndexOf(data, "RTXT"u8.ToArray(), _chunk2SizeOffset);
                if (rtxt < 0) throw new InvalidDataException("TEXTURE: RTXT block not found");

                // version, width, height, depth, mips, ?, format
                Width = (int)ReadU32(data, rtxt + 8);
                Height = (int)ReadU32(data, rtxt + 12);
                var mips = ReadU32(data, rtxt + 20);
                var format = ReadU32(data, rtxt + 28);
                if (format != 6 || mips != 1) throw new InvalidDataException($"TEXTURE: expected DXT5 with 1 mip, got format {format}, mips {mips}");
                if (Width % Cell != 0 || Height % Cell != 0) throw new InvalidDataException($"TEXTURE: size {Width}x{Height} is not a multiple of {Cell}");

                _heightOffset = rtxt + 12;
                var pixelsOffset = data.Length - Width * Height; // DXT5 = 1 byte per pixel
                if (pixelsOffset <= rtxt) throw new InvalidDataException("TEXTURE: unexpected data size");
                _head = data[..pixelsOffset];
                _pixels = data[pixelsOffset..];
            }

            public void AddRows(int pixelRows)
            {
                var grown = new byte[_pixels.Length + Width * pixelRows]; // zero DXT5 blocks = transparent
                Buffer.BlockCopy(_pixels, 0, grown, 0, _pixels.Length);
                _pixels = grown;
                Height += pixelRows;
            }

            // rgba: Cell x Cell, top-down. Placed at memory rows y..y+255 (same orientation as existing icons).
            public void WriteCell(int x, int y, byte[] rgba)
            {
                var blocksPerRow = Width / 4;
                var block = new byte[64];
                for (var by = 0; by < Cell / 4; by++)
                {
                    for (var bx = 0; bx < Cell / 4; bx++)
                    {
                        for (var j = 0; j < 4; j++)
                        {
                            Buffer.BlockCopy(rgba, ((by * 4 + j) * Cell + bx * 4) * 4, block, j * 16, 16);
                        }
                        var o = ((y / 4 + by) * blocksPerRow + x / 4 + bx) * 16;
                        Bc3.EncodeBlock(block, _pixels.AsSpan(o, 16));
                    }
                }
            }

            public byte[] Build()
            {
                var result = new byte[_head.Length + _pixels.Length];
                Buffer.BlockCopy(_head, 0, result, 0, _head.Length);
                Buffer.BlockCopy(_pixels, 0, result, _head.Length, _pixels.Length);
                BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(_heightOffset), (uint)Height);
                BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(_chunk2SizeOffset), (uint)(result.Length - _chunk2SizeOffset - 4));
                return result;
            }
        }

        public sealed record Placement(string Name, uint X, uint Y, bool New);

        // icons: sprite name -> cell x cell RGBA. Returns new TSH + TEXTURE bytes.
        public static (byte[] Tsh, byte[] Texture, List<Placement> Placements, int AddedRows) Build(
            byte[] baseTsh, byte[] baseTexture, IReadOnlyList<(string Name, byte[] Rgba)> icons, int cell)
        {
            Cell = cell;
            var index = new SpriteIndex(baseTsh);
            var sheet = new Sheet(baseTexture);
            var originalHeight = sheet.Height;
            var placements = new List<Placement>();
            var todo = new List<(string Name, byte[] Rgba)>();

            foreach (var icon in icons)
            {
                var existing = index.Sprites.FirstOrDefault(s => string.Equals(s.Name, icon.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null && existing.W == Cell && existing.H == Cell)
                {
                    sheet.WriteCell((int)existing.X, (int)existing.Y, icon.Rgba);
                    placements.Add(new Placement(existing.Name, existing.X, existing.Y, false));
                }
                else
                {
                    todo.Add(icon);
                }
            }

            var free = FreeCells(index, sheet.Width, sheet.Height);
            var columns = sheet.Width / Cell;
            var addedRows = 0;
            if (todo.Count > free.Count)
            {
                addedRows = (todo.Count - free.Count + columns - 1) / columns;
                var oldRows = sheet.Height / Cell;
                sheet.AddRows(addedRows * Cell);
                for (var r = oldRows; r < oldRows + addedRows; r++)
                    for (var c = 0; c < columns; c++)
                        free.Add((c, r));
            }

            for (var i = 0; i < todo.Count; i++)
            {
                var (c, r) = free[i];
                uint x = (uint)(c * Cell), y = (uint)(r * Cell);
                sheet.WriteCell((int)x, (int)y, todo[i].Rgba);
                index.Sprites.Add(new Sprite { Name = todo[i].Name, X = x, Y = y, W = (uint)Cell, H = (uint)Cell });
                placements.Add(new Placement(todo[i].Name, x, y, true));
            }

            return (index.Build(sheet.Width, sheet.Height, sheet.Height == originalHeight), sheet.Build(), placements, addedRows);
        }

        public static List<(int Col, int Row)> FreeCells(SpriteIndex index, int width, int height)
        {
            int columns = width / Cell, rows = height / Cell;
            var used = new bool[columns, rows];
            foreach (var s in index.Sprites)
            {
                if (s.W == 0 || s.H == 0) continue;
                for (var c = (int)(s.X / Cell); c <= (int)((s.X + s.W - 1) / Cell) && c < columns; c++)
                    for (var r = (int)(s.Y / Cell); r <= (int)((s.Y + s.H - 1) / Cell) && r < rows; r++)
                        used[c, r] = true;
            }
            var free = new List<(int, int)>();
            for (var r = 0; r < rows; r++)
                for (var c = 0; c < columns; c++)
                    if (!used[c, r]) free.Add((c, r));
            return free;
        }

        // FNV-1 (32-bit) of the upper-case name, then of the name as written.
        public static byte[] SpriteHash(string name)
        {
            var result = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0), Fnv1(Encoding.UTF8.GetBytes(name.ToUpperInvariant())));
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), Fnv1(Encoding.UTF8.GetBytes(name)));
            return result;
        }

        static uint Fnv1(byte[] bytes)
        {
            var h = 0x811c9dc5u;
            foreach (var b in bytes) h = unchecked(h * 0x01000193u) ^ b;
            return h;
        }

        static uint ReadU32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));

        static void WriteU32(Stream s, uint v)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(b, v);
            s.Write(b);
        }

        static void WriteF32(Stream s, float v)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteSingleBigEndian(b, v);
            s.Write(b);
        }

        static int IndexOf(byte[] data, byte[] pattern, int start)
        {
            var i = data.AsSpan(start).IndexOf(pattern);
            return i >= 0 ? start + i : -1;
        }
    }

    // Small DXT5/BC3 block encoder (range fit). Good enough for UI icons.
    static class Bc3
    {
        // rgba: 16 pixels * 4 bytes, row-major inside the 4x4 block.
        public static void EncodeBlock(byte[] rgba, Span<byte> output)
        {
            // Alpha
            byte aMax = 0, aMin = 255;
            for (var i = 0; i < 16; i++)
            {
                var a = rgba[i * 4 + 3];
                if (a > aMax) aMax = a;
                if (a < aMin) aMin = a;
            }
            output[0] = aMax;
            output[1] = aMin;
            ulong alphaBits = 0;
            if (aMax != aMin)
            {
                Span<int> palette = stackalloc int[8];
                palette[0] = aMax;
                palette[1] = aMin;
                for (var k = 1; k < 7; k++) palette[k + 1] = ((7 - k) * aMax + k * aMin) / 7;
                for (var i = 0; i < 16; i++)
                {
                    int a = rgba[i * 4 + 3], best = 0, bestDist = int.MaxValue;
                    for (var k = 0; k < 8; k++)
                    {
                        var dist = Math.Abs(palette[k] - a);
                        if (dist < bestDist) { bestDist = dist; best = k; }
                    }
                    alphaBits |= (ulong)best << (3 * i);
                }
            }
            for (var i = 0; i < 6; i++) output[2 + i] = (byte)(alphaBits >> (8 * i));

            // Colour: bounding box of visible pixels
            int[] lo = [255, 255, 255], hi = [0, 0, 0];
            var anyVisible = false;
            for (var pass = 0; pass < 2 && !anyVisible; pass++)
            {
                for (var i = 0; i < 16; i++)
                {
                    if (pass == 0 && rgba[i * 4 + 3] == 0) continue;
                    anyVisible = true;
                    for (var c = 0; c < 3; c++)
                    {
                        lo[c] = Math.Min(lo[c], rgba[i * 4 + c]);
                        hi[c] = Math.Max(hi[c], rgba[i * 4 + c]);
                    }
                }
            }

            int c0 = To565(hi), c1 = To565(lo);
            if (c0 < c1) (c0, c1) = (c1, c0);
            uint colorBits = 0;
            if (c0 != c1)
            {
                var p0 = From565(c0);
                var p1 = From565(c1);
                int[][] palette =
                [
                    p0, p1,
                    [(2 * p0[0] + p1[0]) / 3, (2 * p0[1] + p1[1]) / 3, (2 * p0[2] + p1[2]) / 3],
                    [(p0[0] + 2 * p1[0]) / 3, (p0[1] + 2 * p1[1]) / 3, (p0[2] + 2 * p1[2]) / 3],
                ];
                for (var i = 0; i < 16; i++)
                {
                    int best = 0, bestDist = int.MaxValue;
                    for (var k = 0; k < 4; k++)
                    {
                        int dr = palette[k][0] - rgba[i * 4], dg = palette[k][1] - rgba[i * 4 + 1], db = palette[k][2] - rgba[i * 4 + 2];
                        var dist = dr * dr + dg * dg + db * db;
                        if (dist < bestDist) { bestDist = dist; best = k; }
                    }
                    colorBits |= (uint)best << (2 * i);
                }
            }
            BinaryPrimitives.WriteUInt16LittleEndian(output[8..], (ushort)c0);
            BinaryPrimitives.WriteUInt16LittleEndian(output[10..], (ushort)c1);
            BinaryPrimitives.WriteUInt32LittleEndian(output[12..], colorBits);
        }

        static int To565(int[] c) => ((c[0] * 31 + 127) / 255) << 11 | ((c[1] * 63 + 127) / 255) << 5 | ((c[2] * 31 + 127) / 255);

        static int[] From565(int v) => [((v >> 11) & 31) * 255 / 31, ((v >> 5) & 63) * 255 / 63, (v & 31) * 255 / 31];
    }
}
