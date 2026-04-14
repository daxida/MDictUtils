using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Lib.BuildModels;

namespace Lib.Build;

internal partial class KeyBlockIndexBuilder(ILogger<KeyBlockIndexBuilder> logger)
{
    public KeyBlockIndex Build(ReadOnlyCollection<MdxKeyBlock> keyBlocks, int compressionType)
    {
        if (keyBlocks is [])
            return new([], 0);

        var arrayPool = ArrayPool<byte>.Shared;

        int decompDataTotalSize = keyBlocks.Sum(static b => b.IndexEntryLength);
        var decompArray = arrayPool.Rent(decompDataTotalSize);
        var decompData = decompArray.AsSpan(..decompDataTotalSize);

        int maxBlockSize = keyBlocks.Max(static b => b.IndexEntryLength);
        var blockBuffer = maxBlockSize < 256
            ? stackalloc byte[maxBlockSize]
            : new byte[maxBlockSize];

        int bytesWritten = 0;
        foreach (var block in keyBlocks)
        {
            var indexEntry = blockBuffer[..block.IndexEntryLength];
            block.GetIndexEntry(indexEntry);
            LogIndexEntry(indexEntry);
            var destination = decompData.Slice(bytesWritten, indexEntry.Length);
            indexEntry.CopyTo(destination);
            bytesWritten += indexEntry.Length;
        }

        Debug.Assert(bytesWritten == decompDataTotalSize);

        var compressedBytes = MdxBlock.MdxCompress(decompData, compressionType);

        KeyBlockIndex index = new(
            CompressedBytes: compressedBytes,
            DecompSize: bytesWritten);

        arrayPool.Return(decompArray);

        LogIndexBuilt(index.DecompSize, index.CompressedSize);

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
