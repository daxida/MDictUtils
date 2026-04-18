using MDictUtils.BuildModels;

namespace MDictUtils.Build;

internal interface IDataBuilder
{
    public KeyData BuildKeyData(List<MDictEntry> entries, MDictMetadata metadata);
    public RecordData BuildRecordData(List<MDictEntry> entries, MDictMetadata metadata);
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
