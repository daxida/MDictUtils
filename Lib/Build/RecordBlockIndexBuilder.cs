using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class RecordBlockIndexBuilder
{
    public RecordBlockIndex Build(ReadOnlyCollection<MdxRecordBlock> recordBlocks)
    {
        if (recordBlocks is [])
            return new([]);

        int indexSize = recordBlocks.Sum(static b => b.IndexEntryLength);
        var indexBuilder = ImmutableArray.CreateBuilder<byte>(indexSize);

        int maxBlockSize = recordBlocks.Max(static b => b.IndexEntryLength);
        var blockBuffer = maxBlockSize < 256
            ? stackalloc byte[maxBlockSize]
            : new byte[maxBlockSize];

        int bytesWritten = 0;
        foreach (var block in recordBlocks)
        {
            var indexEntry = blockBuffer[..block.IndexEntryLength];
            block.GetIndexEntry(indexEntry);

            indexBuilder.AddRange(indexEntry);
            bytesWritten += indexEntry.Length;
        }
        Debug.Assert(bytesWritten == indexSize);

        return new(indexBuilder.MoveToImmutable());
    }
}
