using System;
using System.Buffers;
using System.Collections.Immutable;

namespace Lib.BuildModels;

/// <summary>
/// Abstract base class for <see cref="MdxRecordBlock"/> and <see cref="MdxKeyBlock"/>.
/// </summary>
internal abstract class MdxBlock
{
    private readonly static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    protected long _decompSize;
    protected ImmutableArray<byte> _compData;
    protected long _compSize;

    protected MdxBlock(ReadOnlySpan<OffsetTableEntry> offsetTableEntries, int compressionType)
    {
        if (compressionType != 2)
            throw new NotSupportedException();

        // Console.WriteLine("[Debug] Calling MdxBlock...");

        long longDecompDataSize = offsetTableEntries.Sum(BlockEntryLength);
        int decompDataSize = Convert.ToInt32(longDecompDataSize);
        var decompData = _arrayPool.Rent(decompDataSize);

        var maxBlockSize = offsetTableEntries.Max(BlockEntryLength);
        var blockBuffer = maxBlockSize < 256
            ? stackalloc byte[(int)maxBlockSize]
            : new byte[maxBlockSize];

        int totalSize = 0;
        foreach (var entry in offsetTableEntries)
        {
            int blockSize = GetBlockEntry(entry, blockBuffer);
            // Console.WriteLine($"[Debug] BlockEntry ({blockEntry.Length} bytes): {BitConverter.ToString(blockEntry)}");
            var source = blockBuffer[..blockSize];
            var destination = decompData.AsSpan(start: totalSize, length: blockSize);
            source.CopyTo(destination);
            totalSize += blockSize;
        }

        // Console.WriteLine("[Debug] Building MdxBlock...");
        _decompSize = totalSize;
        // Console.WriteLine($"[Debug] Decompressed array length (_decompSize): {_decompSize}");
        // Common.PrintPythonStyle(decompArray);

        _compData = MdxCompress(decompData[..totalSize], compressionType);
        _compSize = _compData.Length;
        // Console.WriteLine($"[Debug] Compressed array length (_compSize): {_compSize}");

        _arrayPool.Return(decompData);
    }

    public ReadOnlySpan<byte> BlockData => _compData.AsSpan();

    public abstract void GetIndexEntry(Span<byte> buffer);
    protected abstract int GetBlockEntry(OffsetTableEntry entry, Span<byte> buffer);
    public abstract long BlockEntryLength(OffsetTableEntry entry);
    public abstract int IndexEntryLength { get; }

    // Called in MdxBlock init
    public static ImmutableArray<byte> MdxCompress(ReadOnlySpan<byte> data, int compressionType)
    {
        if (compressionType != 2)
            throw new NotSupportedException("Only compressionType=2 (Zlib) is supported in this version.");

        // Compression type (little-endian)
        Span<byte> lend = stackalloc byte[4];
        Common.ToLittleEndian((uint)compressionType, lend); // <L in Python

        // Adler32 checksum (big-endian)
        uint adler = Common.Adler32(data);
        Span<byte> adlerBytes = stackalloc byte[4];
        Common.ToBigEndian(adler, adlerBytes); // Python uses >L

        // byte[] header = [.. lend, .. adlerBytes];

        // It's possible for compressed data to be larger than the uncompressed.
        // See: https://zlib.net/zlib_tech.html
        // "For the default settings, ... five bytes per 16 KB block (about 0.03%)"
        // So we have to rent a size a little bit larger.
        var buffer = _arrayPool.Rent(data.Length + (data.Length * 5 / 16_000) + 32);

        var size = ZLibCompression.Compress(data, buffer);

        ImmutableArray<byte> compressed = [.. lend, .. adlerBytes, .. buffer.AsSpan(..size)];
        _arrayPool.Return(buffer);

        // Console.WriteLine($"adler: {adler}");
        // Console.WriteLine($"header: {BitConverter.ToString(header)}");

        return compressed;
    }
}
