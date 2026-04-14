using System.Collections.Generic;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class MDictDataBuilder
(
    IMDictWriterLogger logger,
    OffsetTableBuilder offsetTableBuilder,
    KeyBlockIndexBuilder keyBlockIndexBuilder,
    KeyBlockBuilder keyBlockBuilder,
    RecordBlockIndexBuilder recordBlockIndexBuilder,
    RecordBlockBuilder recordBlockBuilder
)
{
    public MDictData BuildData(List<MDictEntry> entries, MDictWriterOptions opt)
    {
        var offsetTable = offsetTableBuilder.Build(entries, opt);
        logger.LogOffsetTable(offsetTable);

        logger.LogBeginBuildingKeyBlocks();
        var keyBlocks = keyBlockBuilder
            .Build(offsetTable, opt.KeySize, opt.CompressionType)
            .AsReadOnly();
        logger.LogKeyBlocks(opt.KeySize, keyBlocks);

        logger.LogBeginBuildingKeybIndex();
        var keyBlockIndex = keyBlockIndexBuilder.Build(keyBlocks, opt.CompressionType);
        logger.LogKeyBlockIndex(keyBlockIndex);

        var recordBlocks = recordBlockBuilder
            .Build(offsetTable, opt.BlockSize, opt.CompressionType)
            .AsReadOnly();
        logger.LogRecordBlocks(recordBlocks);

        var recordBlockIndex = recordBlockIndexBuilder.Build(recordBlocks);
        logger.LogRecordIndex(recordBlockIndex);

        logger.LogInitializationComplete();

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
