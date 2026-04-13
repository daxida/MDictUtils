using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Lib;

internal static class ZLibCompression
{
    public static void Decompress(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // The .ToArray() allocation here is unfortunately unavoidable.
        // See: https://github.com/dotnet/runtime/issues/24622
        // Unless we want to enable the "unsafe" compiler flag.
        // See: https://stackoverflow.com/a/48223990
        using var ms = new MemoryStream(input.ToArray());
        using var z = new ZLibStream(ms, CompressionMode.Decompress);

        z.ReadExactly(output);

        if (z.ReadByte() is not -1)
        {
            throw new OverflowException($"More than expected {output.Length} bytes in decompression stream");
        }
    }

    #if false
    public static unsafe void DecompressUnsafe(ReadOnlySpan<byte> input, Span<byte> output)
    {
        fixed (byte* pBuffer = &MemoryMarshal.GetReference(input))
        {
            using var ms = new UnmanagedMemoryStream(pBuffer, input.Length, input.Length, FileAccess.Read);
            using var z = new ZLibStream(ms, CompressionMode.Decompress);

            z.ReadExactly(output);

            if (z.ReadByte() is not -1)
            {
                throw new OverflowException($"More than expected {output.Length} bytes in decompression stream");
            }
        }
    }
    #endif

    /// <remarks>
    /// python default is -1 == 6 , see: https://docs.python.org/3/library/zlib.html#zlib.Z_DEFAULT_COMPRESSION
    /// c# are cooked, custom-made levels, and may not correspond to anything
    /// https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.compressionlevel?view=net-10.0
    ///
    /// There is no reliable way to get the same exact bytes, so live with that
    /// </remarks>
    public static int Compress(ReadOnlySpan<byte> input, byte[] output)
    {
        using var ms = new MemoryStream(output);
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(input);
        }
        return Convert.ToInt32(ms.Length);
    }

    #if false
    public static unsafe int CompressUnsafe(ReadOnlySpan<byte> input, Span<byte> output)
    {
        fixed (byte* pBuffer = &MemoryMarshal.GetReference(output))
        {
            using var ms = new UnmanagedMemoryStream(pBuffer, output.Length, output.Length, FileAccess.Write);
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                z.Write(input);
            }
            return Convert.ToInt32(ms.Length);
        }
    }
    #endif
}
