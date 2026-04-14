using System;
using System.Collections.Generic;
using Lib.BuildModels;

namespace Lib.Build;

internal abstract class BlockBuilder<T> where T : MdxBlock
{
    protected abstract T BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries, int compressionType);
    protected abstract long EntryLength(OffsetTableEntry entry);

    public List<T> Build(OffsetTable offsetTable, int blockSize, int compressionType)
    {
        var blocks = new List<T>();
        int thisBlockStart = 0;
        long curSize = 0;

        for (int ind = 0; ind <= offsetTable.Entries.Length; ind++)
        {
            var offsetTableEntry = (ind == offsetTable.Entries.Length)
                ? null
                : offsetTable.Entries[ind];

            bool flush = false;

            if (ind == 0)
            {
                flush = false;
            }
            else if (offsetTableEntry == null)
            {
                flush = true;
            }
            else if (curSize + EntryLength(offsetTableEntry) > blockSize)
            {
                flush = true;
            }

            if (flush)
            {
                var blockEntries = offsetTable.Entries.AsSpan(thisBlockStart..ind);
                // foreach (var entry in blockEntries)
                // {
                //     Console.WriteLine($"[split flush] {entry}");
                // }
                var block = BlockConstructor(blockEntries, compressionType);
                blocks.Add(block);
                curSize = 0;
                thisBlockStart = ind;
            }

            if (offsetTableEntry != null)
            {
                curSize += EntryLength(offsetTableEntry);
            }
        }

        return blocks;
    }
}
