using System;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class KeyBlockBuilder : BlockBuilder<MdxKeyBlock>
{
    protected override MdxKeyBlock BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries, int compressionType)
        => new(entries, compressionType);

    protected override long EntryLength(OffsetTableEntry entry)
        => entry.MdxKeyBlockEntryLength;
}
