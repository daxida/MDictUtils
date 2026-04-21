using System.Buffers;
using System.Diagnostics;
using MDictUtils.BuildModels;
using MDictUtils.Extensions;
using Microsoft.Extensions.Logging;

namespace MDictUtils.Build.Index;

internal sealed partial class KeyBlockIndexBuilder
(
    ILogger<KeyBlockIndexBuilder> logger,
    IBlockCompressor blockCompressor
)
{
    private static readonly MemoryPool<byte> _arrayPool = MemoryPool<byte>.Shared;

    public async Task<CompressedBlock> BuildAsync(ImmutableArray<KeyBlock> keyBlocks)
    {
        if (keyBlocks is [])
            return new([], 0);

        int totalSize = keyBlocks.Sum(static b => b.IndexEntryLength);
        var uncompressed = _arrayPool.Rent(totalSize);

        int position = 0;
        foreach (var block in keyBlocks)
        {
            var size = block.IndexEntryLength;
            var buffer = uncompressed.Memory.Slice(position, size).Span;
            block.CopyIndexEntryTo(buffer);
            LogIndexEntry(buffer);
            position += size;
        }

        var compressed = await blockCompressor
            .CompressAsync(uncompressed.Memory[..position]);

        uncompressed.Dispose();

        CompressedBlock index = new(
            Bytes: compressed,
            DecompSize: position);

        Debug.Assert(position == totalSize);
        LogIndexBuilt(index.DecompSize, index.Size);

        return index;
    }

    [Conditional("DEBUG")]
    private void LogIndexEntry(ReadOnlySpan<byte> indexEntry)
    {
        var bytes = new string[indexEntry.Length];
        for (int i = 0; i < indexEntry.Length; i++)
        {
            bytes[i] = $"{indexEntry[i]:X2}";
        }
        var entryData = string.Join(" ", bytes);
        LogEntryData(entryData);
    }

    [LoggerMessage(LogLevel.Debug, "KeyBlock index entry: {EntryData}")]
    private partial void LogEntryData(string entryData);

    [LoggerMessage(LogLevel.Debug,
    "Key index built: decompressed={DecompSize}, compressed={CompSize}")]
    private partial void LogIndexBuilt(long decompSize, int compSize);
}
