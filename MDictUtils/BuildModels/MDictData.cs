using System.Diagnostics;
using System.Text;

namespace MDictUtils.BuildModels;

internal sealed record DesiredKeyBlockSize(int Value);
internal sealed record DesiredRecordBlockSize(int Value);

internal sealed record EncodingSettings
{
    public Encoding Encoding { get; }
    public int EncodingLength { get; }
    public EncodingSettings(Encoding encoding, bool isMdd)
    {
        if (isMdd || encoding == Encoding.Unicode)
        {
            Encoding = Encoding.Unicode;
            EncodingLength = 2;
        }
        else if (encoding == Encoding.UTF8)
        {
            Encoding = Encoding.UTF8;
            EncodingLength = 1;
        }
        else
        {
            throw new NotSupportedException("Unknown encoding. Supported: utf8, utf16");
        }
    }
}

internal readonly record struct KeyData
(
    int EntryCount,
    CompressedBlock KeyBlockIndex,
    ImmutableArray<KeyBlock> KeyBlocks
)
{
    public int KeyBlocksSize => KeyBlocks.Sum(static b => b.Bytes.Length);
}

internal readonly record struct RecordData
(
    int EntryCount,
    Block RecordBlockIndex,
    ImmutableArray<RecordBlock> RecordBlocks
)
{
    public int RecordBlocksSize => RecordBlocks.Sum(static b => b.Bytes.Length);
}


internal readonly record struct Block(ImmutableArray<byte> Bytes)
{
    public int Size => Bytes.Length;
}

internal readonly record struct CompressedBlock(ImmutableArray<byte> Bytes, long DecompSize)
{
    public int Size => Bytes.Length;
}

internal readonly record struct OffsetTable
(
    ImmutableArray<OffsetTableEntry> Entries,
    ImmutableArray<Range> KeyBlockRanges,
    ImmutableArray<Range> RecordBlockRanges
)
{
    public int Length => Entries.Length;
    public ReadOnlySpan<OffsetTableEntry> AsSpan(Range range) => Entries.AsSpan(range);
    public Dictionary<string, int> GetFilePathToTotalEntryCount()
    {
        var dict = new Dictionary<string, int>();
        foreach (var entry in Entries)
        {
            dict[entry.FilePath] =
                dict.TryGetValue(entry.FilePath, out var count)
                    ? count + 1
                    : 1;
        }
        return dict;
    }
}
