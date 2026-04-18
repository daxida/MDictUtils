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
    private OffsetTable? _offsetTable;

    public KeyData BuildKeyData(List<MDictEntry> entries, MDictMetadata m)
    {
        _offsetTable = offsetTableBuilder
            .Build(entries);

        var keyBlocks = keyBlocksBuilder
            .Build(_offsetTable.Value);

        var keyBlockIndex = keyBlockIndexBuilder
            .Build(keyBlocks);

        return new KeyData
        (
            entries.Count,
            keyBlockIndex,
            keyBlocks
        );
    }

    public RecordData BuildRecordData(List<MDictEntry> entries, MDictMetadata m)
    {
        if (_offsetTable is null)
            throw new InvalidOperationException("Must build key data before record data.");

        var recordBlocks = recordBlocksBuilder
            .Build(_offsetTable.Value);

        var recordBlockIndex = recordBlockIndexBuilder
            .Build(recordBlocks);

        return new RecordData
        (
            entries.Count,
            recordBlockIndex,
            recordBlocks
        );
    }
}
