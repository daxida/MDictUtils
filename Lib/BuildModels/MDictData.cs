using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lib.BuildModels;

internal sealed record MDictData
(
    MDictMetadata Metadata,
    int EntryCount,
    OffsetTable OffsetTable,
    ReadOnlyCollection<MdxKeyBlock> KeyBlocks,
    ReadOnlyCollection<MdxRecordBlock> RecordBlocks,
    KeyBlockIndex KeyBlockIndex,
    RecordBlockIndex RecordBlockIndex
);

internal sealed record OffsetTable(ImmutableArray<OffsetTableEntry> Entries)
{
    public long TotalRecordLength => Entries.Sum(static e => e.RecordSize);
}

internal sealed record KeyBlockIndex(ImmutableArray<byte> CompressedBytes, long DecompSize)
{
    public int CompressedSize => CompressedBytes.Length;
}

internal sealed record RecordBlockIndex(ImmutableArray<byte> Bytes)
{
    public int Size => Bytes.Length;
}
