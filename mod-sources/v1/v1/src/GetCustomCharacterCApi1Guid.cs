using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static NuGuid.NativeData GetCustomCharacterCApi1Guid(string customCharacterId)
    {
        // UUID v5 with OID namespace
        byte[] sourceArray = SHA1.HashData(
            new byte[] {
                0x6b, 0xa7, 0xb8, 0x12, 0x9d, 0xad, 0x11, 0xd1, 0x80, 0xb4, 0x00, 0xc0, 0x4f, 0xd4, 0x30, 0xc8,
            }
            .Concat(
                Encoding.UTF8.GetBytes(customCharacterId)
            )
            .ToArray()
        );

        byte[] array = new byte[16];
        Array.Copy(sourceArray, array, 16);
        array[6] &= 15;
        array[6] |= 80;
        array[8] &= 63;
        array[8] |= 128;

        return new NuGuid.NativeData
        {
            V1 = array[0],
            V2 = array[1],
            V3 = array[2],
            V4 = array[3],
            V5 = array[4],
            V6 = array[5],
            V7 = array[6],
            V8 = array[7],
            V9 = array[8],
            V10 = array[9],
            V11 = array[10],
            V12 = array[11],
            V13 = array[12],
            V14 = array[13],
            V15 = array[14],
            V16 = array[15],
        };
    }
}