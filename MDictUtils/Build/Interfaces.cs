using MDictUtils.BuildModels;

namespace MDictUtils.Build;

internal interface IDataBuilder
{
    public KeyData BuildKeyData(List<MDictEntry> entries);
    public RecordData BuildRecordData(List<MDictEntry> entries);
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
