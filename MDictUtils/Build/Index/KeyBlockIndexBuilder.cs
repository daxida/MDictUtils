using System.Buffers;
using System.Diagnostics;
using MDictUtils.BuildModels;
using MDictUtils.Extensions;
using Microsoft.Extensions.Logging;

namespace MDictUtils.Build.Index;

internal partial class KeyBlockIndexBuilder
(
    ILogger<KeyBlockIndexBuilder> logger,
    IBlockCompressor blockCompressor
)
{
    private readonly static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public CompressedBlock Build(ImmutableArray<KeyBlock> keyBlocks)
    {
        if (keyBlocks is [])
            return new([], 0);

        int decompDataTotalSize = keyBlocks.Sum(static b => b.IndexEntryLength);
        var decompArray = _arrayPool.Rent(decompDataTotalSize);

        int bytesWritten = 0;
        foreach (var block in keyBlocks)
        {
            var buffer = decompArray.AsSpan(bytesWritten, block.IndexEntryLength);
            block.CopyIndexEntryTo(buffer);
            LogIndexEntry(buffer);
            bytesWritten += block.IndexEntryLength;
        }

        var decompData = decompArray.AsSpan(..bytesWritten);
        var compressedBytes = blockCompressor.Compress(decompData);
        _arrayPool.Return(decompArray);

        CompressedBlock index = new(
            Bytes: compressedBytes,
            DecompSize: bytesWritten);

        Debug.Assert(bytesWritten == decompDataTotalSize);
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

    [LoggerMessage(LogLevel.Debug, "Entry: {EntryData}")]
    private partial void LogEntryData(string entryData);

    [LoggerMessage(LogLevel.Debug,
    "Key index built: decompressed={DecompSize}, compressed={CompSize}")]
    private partial void LogIndexBuilt(long decompSize, int compSize);
}
