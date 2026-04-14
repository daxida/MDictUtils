using System;
using Microsoft.Extensions.Logging;
using Lib.BuildModels;

namespace Lib.Build;

internal sealed class KeyBlockBuilder(ILogger<KeyBlockBuilder> logger) : BlockBuilder<MdxKeyBlock>(logger)
{
    protected override MdxKeyBlock BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries, int compressionType)
        => new(entries, compressionType);

    protected override long EntryLength(OffsetTableEntry entry)
        => entry.MdxKeyBlockEntryLength;
}
