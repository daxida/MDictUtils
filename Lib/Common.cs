using System;
using System.IO;
using System.Linq;

namespace Lib;

/// <summary>
/// Some helper methods
/// </summary>
internal static class Common
{
    public static ReadOnlySpan<byte> ToBigEndian(Span<byte> bytes)
    {
        if (BitConverter.IsLittleEndian) bytes.Reverse();
        return bytes;
    }

    public static ReadOnlySpan<byte> ToLittleEndian(Span<byte> bytes)
    {
        if (!BitConverter.IsLittleEndian) bytes.Reverse();
        return bytes;
    }

    public static ReadOnlySpan<byte> ToBigEndian(ulong value) => ToBigEndian(BitConverter.GetBytes(value));
    public static ReadOnlySpan<byte> ToBigEndian(uint value) => ToBigEndian(BitConverter.GetBytes(value));
    public static ReadOnlySpan<byte> ToBigEndian(ushort value) => ToBigEndian(BitConverter.GetBytes(value));

    public static ReadOnlySpan<byte> ToLittleEndian(ulong value) => ToLittleEndian(BitConverter.GetBytes(value));
    public static ReadOnlySpan<byte> ToLittleEndian(uint value) => ToLittleEndian(BitConverter.GetBytes(value));
    public static ReadOnlySpan<byte> ToLittleEndian(ushort value) => ToLittleEndian(BitConverter.GetBytes(value));

    public static int ReadInt32BigEndian(BinaryReader br)
        => BitConverter.ToInt32(ToBigEndian(br.ReadBytes(4)));

    public static int ReadUInt16BigEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        Span<byte> slice = stackalloc byte[2];
        buffer.Slice(offset, 2).CopyTo(slice);
        return BitConverter.ToUInt16(ToBigEndian(slice));
    }

    public static uint ReadUInt32BigEndian(Span<byte> bytes)
        => BitConverter.ToUInt32(ToBigEndian(bytes));

    public static void PrintPythonStyle(byte[] data)
    {
        Console.WriteLine("        " + string.Join(" ", data.Select(b => b.ToString("X2"))));

        string pythonStyle = "b'" + string.Concat(data.Select(b =>
        {
            if (b >= 0x20 && b <= 0x7E)
            {
                if (b == (byte)'\\' || b == (byte)'\'')
                    return "\\" + (char)b;
                else
                    return ((char)b).ToString();
            }
            else
            {
                return "\\x" + b.ToString("x2");
            }
        })) + "'";

        Console.WriteLine("        " + pythonStyle);
    }

    // Check zlib implementation...
    //
    // https://github.com/madler/zlib/blob/f9dd6009be3ed32415edf1e89d1bc38380ecb95d/adler32.c#L128
    // https://gist.github.com/AristurtleDev/316358b3f87fd995923b79350be342f5
    //
    // header = (struct.pack(b"<L", compression_type) + 
    //          struct.pack(b">L", zlib.adler32(data) & 0xffffffff)) #depending on python version, zlib.adler32 may return a signed number. 
    private const uint BASE = 65521;
    private const int NMAX = 5552;

    public static uint Adler32(ReadOnlySpan<byte> buf)
    {
        uint adler = 1;
        uint sum2 = 0;

        int len = buf.Length;
        int index = 0;

        while (len > 0)
        {
            int blockLen = len < NMAX ? len : NMAX;
            len -= blockLen;

            while (blockLen >= 16)
            {
                for (int i = 0; i < 16; i++)
                {
                    adler += buf[index++];
                    sum2 += adler;
                }
                blockLen -= 16;
            }

            while (blockLen-- > 0)
            {
                adler += buf[index++];
                sum2 += adler;
            }

            adler %= BASE;
            sum2 %= BASE;
        }

        return (sum2 << 16) | adler;
    }

    public static bool All<T>(this ReadOnlySpan<T> values, Func<T, bool> predicate)
    {
        foreach (var value in values)
        {
            if (!predicate(value))
            {
                return false;
            }
        }
        return true;
    }
}
