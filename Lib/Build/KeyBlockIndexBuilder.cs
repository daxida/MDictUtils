using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class KeyBlockIndexBuilder(IMDictWriterLogger logger)
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
            logger.LogIndexEntry(indexEntry);

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

        return index;
    }
}
