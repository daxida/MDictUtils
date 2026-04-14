using System;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class RecordBlockBuilder : BlockBuilder<MdxRecordBlock>
{
    protected override MdxRecordBlock BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries, int compressionType)
        => new(entries, compressionType);

    protected override long EntryLength(OffsetTableEntry entry)
        => entry.MdxRecordBlockEntryLength;
}
