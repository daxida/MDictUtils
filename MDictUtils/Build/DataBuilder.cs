using System.Threading.Channels;
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

    public async Task ReadRecordBlocksAsync(OffsetTable offsetTable, Channel<(int, RecordBlock)> channel)
        => await recordBlocksBuilder.ReadAsync(offsetTable, channel);
}
