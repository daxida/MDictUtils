using System;
using System.Collections.Generic;

namespace Lib;

internal sealed class MDictWriterLogger
{
    public bool Enabled { get; init; }

    private static void WriteSeparator()
        => Console.Error.WriteLine("=========================");

    private static void WriteMessage(string message)
        => Console.Error.WriteLine($"[Writer] {message}");

    public void LogOffsetTable(IReadOnlyList<OffsetTableEntry> table, long totalRecordLen)
    {
        if (!Enabled) return;
        WriteMessage("Offset table built.");
        WriteMessage($"Total entries: {table.Count}, record length {totalRecordLen}");
        WriteSeparator();
    }

    public void LogBeginBuildingKeyBlocks()
    {
        if (!Enabled) return;
        WriteMessage("Building KeyBlocks");
    }

    public void LogKeyBlocks(int blockSize, IReadOnlyList<MdxKeyBlock> keyBlocks)
    {
        if (!Enabled) return;
        WriteMessage($"Block size set to {blockSize}");
        WriteMessage($"Built {keyBlocks.Count} key blocks.");
        foreach (var keyBlock in keyBlocks)
        {
            Console.Error.WriteLine($"* KeyBlock: {keyBlock}");
        }
    }

    public void LogBlockSizeReset(int blockSize)
    {
        if (!Enabled) return;
        WriteMessage($"Block size reset to {blockSize}");
        WriteSeparator();
    }

    public void LogBeginBuildingKeybIndex()
    {
        if (!Enabled) return;
        WriteMessage("Building KeybIndex");
    }

    public void LogIndexEntry(ReadOnlySpan<byte> indexEntry)
    {
        if (!Enabled) return;
        var bytes = new string[indexEntry.Length];
        for (int i = 0; i < indexEntry.Length; i++)
        {
            bytes[i] = $"{indexEntry[i]:X2}";
        }
        var displayBytes = string.Join(" ", bytes);
        Console.Error.WriteLine($"entry {displayBytes}");
    }

    public void LogKeybIndex(long decompressedSize, long compressedSize)
    {
        if (!Enabled) return;
        WriteMessage($"Key index built: decompressed={decompressedSize}, compressed={compressedSize}");
        WriteSeparator();
    }

    public void LogRecordBlocks(IReadOnlyList<MdxRecordBlock> recordBlocks)
    {
        if (!Enabled) return;
        WriteMessage($"Built {recordBlocks.Count} record blocks.");
        WriteMessage($"Built {recordBlocks}."); // TODO: this only prints the type of the collection.
        WriteSeparator();
    }

    public void LogRecordIndex(long size)
    {
        if (!Enabled) return;
        WriteMessage($"Record index built: size={size}");
        WriteSeparator();
    }

    public void LogInitializationComplete()
    {
        if (!Enabled) return;
        WriteMessage("Initialization complete.\n");
    }
}
