using System.Threading.Channels;
using MDictUtils.BuildModels;

namespace MDictUtils.Build;

internal interface IDataBuilder
{
    public OffsetTable BuildOffsetTable(List<MDictEntry> entries);
    public KeyData BuildKeyData(OffsetTable offsetTable);
    public Task BuildRecordBlocksAsync(OffsetTable offsetTable, Channel<(int, RecordBlock)> channel);
}

internal interface IBlockCompressor
{
    ImmutableArray<byte> Compress(ReadOnlySpan<byte> data);
}

internal interface IRecordBlocksBuilder
{
    Task BuildAsync(OffsetTable offsetTable, Channel<(int, RecordBlock)> channel);
}

internal interface IKeyComparer
{
    int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2);
}
