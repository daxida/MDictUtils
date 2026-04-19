using MDictUtils.Build.Blocks;
using MDictUtils.Build.Index;
using MDictUtils.Build.Offset;
using MDictUtils.BuildModels;

namespace MDictUtils.Build;

internal sealed class DataBuilder
(
    OffsetTableBuilder offsetTableBuilder,
    KeyBlockIndexBuilder keyBlockIndexBuilder,
    KeyBlocksBuilder keyBlocksBuilder,
    RecordBlockIndexBuilder recordBlockIndexBuilder,
    IRecordBlocksBuilder recordBlocksBuilder
)
    : IDataBuilder
{
    public OffsetTable BuildOffsetTable(List<MDictEntry> entries)
        => offsetTableBuilder.Build(entries);

    public KeyData BuildKeyData(OffsetTable offsetTable)
    {
        var keyBlocks = keyBlocksBuilder
            .Build(offsetTable);

        var keyBlockIndex = keyBlockIndexBuilder
            .Build(keyBlocks);

        return new KeyData(offsetTable.Length, keyBlockIndex, keyBlocks);
    }

    public RecordData BuildRecordData(OffsetTable offsetTable)
    {
        var recordBlocks = recordBlocksBuilder
            .Build(offsetTable);

        var recordBlockIndex = recordBlockIndexBuilder
            .Build(recordBlocks);

        return new RecordData(offsetTable.Length, recordBlockIndex, recordBlocks);
    }
}
