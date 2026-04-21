using System.Buffers;

namespace MDictUtils.Build.Compression;

internal sealed class ZLibBlockCompressor : IBlockCompressor
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public async Task<ImmutableArray<byte>> CompressAsync(ReadOnlyMemory<byte> data)
    {
        // It's possible for compressed data to be larger than the uncompressed.
        // See: https://zlib.net/zlib_tech.html
        // "For the default settings, ... five bytes per 16 KB block (about 0.03%)"
        // So we have to rent a size a little bit larger.
        var buffer = _arrayPool.Rent(data.Length + (data.Length * 5 / 16_000) + 32);

        var size = await ZLibCompression.CompressAsync(data, buffer);

        /// <see cref="MDict.DecodeKeyBlockInfo"/>
        /// CompressionType = 2 expressed in little-endian bytes.
        ReadOnlySpan<byte> compressionTypeBytes = [0x02, 0x00, 0x00, 0x00];

        uint checksum = Common.Adler32(data.Span);
        Span<byte> checksumBytes = stackalloc byte[4];
        Common.ToBigEndian(checksum, checksumBytes);

        ImmutableArray<byte> compressed =
        [
            .. compressionTypeBytes,
            .. checksumBytes,
            .. buffer.AsSpan(..size),
        ];

        _arrayPool.Return(buffer);

        return compressed;
    }
}
