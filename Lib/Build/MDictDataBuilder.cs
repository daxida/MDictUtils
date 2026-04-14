using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Lib.Build;

internal sealed class MDictDataBuilder
(
    ILogger<MDictDataBuilder> logger,
    OffsetTableBuilder offsetTableBuilder,
    KeyBlockIndexBuilder keyBlockIndexBuilder,
    KeyBlockBuilder keyBlockBuilder,
    RecordBlockIndexBuilder recordBlockIndexBuilder,
    RecordBlockBuilder recordBlockBuilder
)
    : IMDictDataBuilder
{
    public MDictData BuildData(List<MDictEntry> entries, MDictMetadata m)
    {
        var offsetTable = offsetTableBuilder
            .Build(entries, m);

        var keyBlocks = keyBlockBuilder
            .Build(offsetTable, m.KeySize, m.CompressionType)
            .AsReadOnly();

        var keyBlockIndex = keyBlockIndexBuilder
            .Build(keyBlocks, m.CompressionType);

        var recordBlocks = recordBlockBuilder
            .Build(offsetTable, m.BlockSize, m.CompressionType)
            .AsReadOnly();

        var recordBlockIndex = recordBlockIndexBuilder
            .Build(recordBlocks);

        logger.LogDebug("Initialization complete.");

        return new
        (
            Metadata: m,
            EntryCount: entries.Count,
            OffsetTable: offsetTable,
            KeyBlocks: keyBlocks,
            RecordBlocks: recordBlocks,
            KeyBlockIndex: keyBlockIndex,
            RecordBlockIndex: recordBlockIndex
        );
    }
}
