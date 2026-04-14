using System;
using Microsoft.Extensions.Logging;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class RecordBlockBuilder(ILogger<RecordBlockBuilder> logger) : BlockBuilder<MdxRecordBlock>(logger)
{
    protected override MdxRecordBlock BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries, int compressionType)
        => new(entries, compressionType);

    protected override long EntryLength(OffsetTableEntry entry)
        => entry.MdxRecordBlockEntryLength;
}
