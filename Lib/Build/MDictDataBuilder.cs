using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Lib.BuildModels;

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
    public MDictData BuildData(List<MDictEntry> entries, MDictWriterOptions opt)
    {
        var offsetTable = offsetTableBuilder.Build(entries, opt);

        var keyBlocks = keyBlockBuilder
            .Build(offsetTable, opt.KeySize, opt.CompressionType)
            .AsReadOnly();

        var keyBlockIndex = keyBlockIndexBuilder.Build(keyBlocks, opt.CompressionType);

        var recordBlocks = recordBlockBuilder
            .Build(offsetTable, opt.BlockSize, opt.CompressionType)
            .AsReadOnly();

        var recordBlockIndex = recordBlockIndexBuilder.Build(recordBlocks);

        logger.LogDebug("Initialization complete.");

        return new
        (
            Metadata: opt,
            EntryCount: entries.Count,
            OffsetTable: offsetTable,
            KeyBlocks: keyBlocks,
            RecordBlocks: recordBlocks,
            KeyBlockIndex: keyBlockIndex,
            RecordBlockIndex: recordBlockIndex
        );
    }
}
