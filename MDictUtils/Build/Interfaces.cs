using MDictUtils.BuildModels;

namespace MDictUtils.Build;

internal interface IDataBuilder
{
    public OffsetTable BuildOffsetTable(List<MDictEntry> entries);
    public KeyData BuildKeyData(OffsetTable offsetTable);
    public RecordData BuildRecordData(OffsetTable offsetTable);
}

internal interface IBlockCompressor
{
    ImmutableArray<byte> Compress(ReadOnlySpan<byte> data);
}

internal interface IRecordBlocksBuilder
{
    ImmutableArray<RecordBlock> Build(OffsetTable offsetTable);
}

internal interface IKeyComparer
{
    int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2);
}
