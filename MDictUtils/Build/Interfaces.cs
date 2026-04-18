using MDictUtils.BuildModels;

namespace MDictUtils.Build;

public interface IMDictWriter
{
    public void Write(List<MDictEntry> entries, string outputFile, MDictMetadata? metadata = null);
}

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
    ImmutableArray<RecordBlock> Build(OffsetTable offsetTable, int desiredBlockSize);
}

internal interface IKeyComparer
{
    int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2);
}
