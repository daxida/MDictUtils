using System;
using System.Linq;
using System.Numerics;

namespace Lib;

/// <summary>
/// Some helper methods
/// </summary>
internal static class Common
{
    // To simplify much of this, maybe we can use:
    // https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary?view=net-10.0

    public static void ToLittleEndian(uint input, Span<byte> output)
    {
        if (output.Length != 4)
            throw new ArgumentException("Wrong size buffer", nameof(output));
        if (!BitConverter.TryWriteBytes(output, input))
            throw new ArgumentException($"Could not convert value {input} to bytes");
        if (!BitConverter.IsLittleEndian)
            output.Reverse();
    }

    public static void ToBigEndian(ulong input, Span<byte> output)
    {
        if (output.Length != 8)
            throw new ArgumentException("Wrong size buffer", nameof(output));
        if (!BitConverter.TryWriteBytes(output, input))
            throw new ArgumentException($"Could not convert value {input} to bytes");
        if (BitConverter.IsLittleEndian)
            output.Reverse();
    }

    public static void ToBigEndian(uint input, Span<byte> output)
    {
        if (output.Length != 4)
            throw new ArgumentException("Wrong size buffer", nameof(output));
        if (!BitConverter.TryWriteBytes(output, input))
            throw new ArgumentException($"Could not convert value {input} to bytes");
        if (BitConverter.IsLittleEndian)
            output.Reverse();
    }

    public static void ToBigEndian(ushort input, Span<byte> output)
    {
        if (output.Length != 2)
            throw new ArgumentException("Wrong size buffer", nameof(output));
        if (!BitConverter.TryWriteBytes(output, input))
            throw new ArgumentException($"Could not convert value {input} to bytes");
        if (BitConverter.IsLittleEndian)
            output.Reverse();
    }

    public static T ReadBigEndian<T>(ReadOnlySpan<byte> input, bool isUnsigned)
        where T : unmanaged, IBinaryInteger<T>
        => T.ReadBigEndian(input, isUnsigned);

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

    public static Func<int, Range> RangeIncrementor()
    {
        int start = 0;
        Range getNewRange(int length)
        {
            int end = start + length;
            Range range = new(start, end);
            start = end;
            return range;
        }
        return getNewRange;
    }
}
