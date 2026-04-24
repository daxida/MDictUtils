using System.Buffers;
using MDictUtils.BuildModels;

namespace MDictUtils.Build.Compression;

internal sealed class NoneBlockCompressor : IBlockCompressor
{
    private static readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;

    /// <summary>
    /// CompressionType = 0 expressed in four bytes.
    /// </summary>
    private static ReadOnlySpan<byte> CompressionTypeBytes => [0x00, 0x00, 0x00, 0x00];

    public async Task<CompressedBlock> CompressAsync(ReadOnlyMemory<byte> data)
    {
        uint checksum = Common.Adler32(data.Span);

        var compressedSize = data.Length + 8;
        var memoryOwner = _memoryPool.Rent(compressedSize);
        var buffer = memoryOwner.Memory.Span;

        CompressionTypeBytes.CopyTo(buffer[0..4]);
        Common.ToBigEndian(checksum, buffer[4..8]);
        data.Span.CopyTo(buffer[8..compressedSize]);

        return new(memoryOwner, compressedSize, data.Length);
    }
}
