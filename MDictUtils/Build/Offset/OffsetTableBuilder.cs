using System.Buffers;
using MDictUtils.BuildModels;
using MDictUtils.Extensions;
using Microsoft.Extensions.Logging;

namespace MDictUtils.Build.Offset;

internal sealed partial class OffsetTableBuilder
(
    ILogger<OffsetTableBuilder> logger,
    IKeyComparer keyComparer,
    DesiredKeyBlockSize desiredKeyBlockSize,
    DesiredRecordBlockSize desiredRecordBlockSize,
    EncodingSettings encoder
)
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private readonly static ArrayPool<Range> _rangePool = ArrayPool<Range>.Shared;

    public OffsetTable Build(List<MDictEntry> entries)
    {
        entries.Sort((a, b) => keyComparer.Compare(a.Key, b.Key));

        var tableEntries = GetTableEntries(entries);
        var keyBlockRanges = GetKeyBlockRanges(tableEntries);
        var recordBlockRanges = GetRecordBlockRanges(tableEntries);

        return new OffsetTable(tableEntries, keyBlockRanges, recordBlockRanges);
    }

    private ImmutableArray<OffsetTableEntry> GetTableEntries(List<MDictEntry> entries)
    {
        var arrayBuilder = ImmutableArray.CreateBuilder<OffsetTableEntry>(entries.Count);
        long currentOffset = 0;
        int maxEncLength = GetMaxEncLength(entries, encoder);

        byte[]? bufferArray = null;
        var buffer = maxEncLength < 256
            ? stackalloc byte[maxEncLength]
            : _arrayPool.Rent(maxEncLength, ref bufferArray);

        foreach (var entry in entries)
        {
            var length = encoder.Encoding.GetBytes($"{entry.Key}\0", buffer);
            var keyNull = ImmutableArray.Create(buffer[..length]);

            // Subtract the encoding length because we appended '\0'
            var keyLen = (length - encoder.EncodingLength) / encoder.EncodingLength;

            var tableEntry = new OffsetTableEntry
            {
                NullTerminatedKeyBytes = keyNull,
                KeyCharacterCount = keyLen,
                Offset = currentOffset,
                RecordSize = entry.Size,
                RecordPos = entry.Pos,
                FilePath = entry.Path,
            };
            arrayBuilder.Add(tableEntry);

            currentOffset += entry.Size;
        }

        if (bufferArray is not null)
            _arrayPool.Return(bufferArray);

        var tableEntries = arrayBuilder.MoveToImmutable();
        LogInfo(tableEntries.Length, currentOffset);

        return tableEntries;
    }

    private static int GetMaxEncLength(List<MDictEntry> entries, EncodingSettings encoder)
    {
        int maxEncLength = 0;
        foreach (var entry in entries)
        {
            int encLength = encoder.Encoding.GetByteCount(entry.Key);
            maxEncLength = int.Max(maxEncLength, encLength);
        }
        maxEncLength += encoder.EncodingLength; // Because we'll be appending an extra '\0' character.
        return maxEncLength;
    }

    private ImmutableArray<Range> GetKeyBlockRanges(ImmutableArray<OffsetTableEntry> tableEntries)
    {
        var keyEntrySizes = tableEntries
            .Select(static e => e.KeyDataSize)
            .ToArray();
        return PartitionTable(keyEntrySizes, desiredKeyBlockSize.Value);
    }

    private ImmutableArray<Range> GetRecordBlockRanges(ImmutableArray<OffsetTableEntry> tableEntries)
    {
        var recordEntrySizes = tableEntries
            .Select(static e => e.RecordSize)
            .ToArray();
        return PartitionTable(recordEntrySizes, desiredRecordBlockSize.Value);
    }

    private ImmutableArray<Range> PartitionTable(ReadOnlySpan<int> entrySizes, int desiredBlockSize)
    {
        var ranges = _rangePool.Rent(entrySizes.Length);
        int start = 0;
        int blockCount = 0;
        long blockSize = 0;

        for (int end = 0; end <= entrySizes.Length; end++)
        {
            int? entrySize = (end == entrySizes.Length)
                ? null
                : entrySizes[end];

            bool flush;
            if (end == 0)
                flush = false;
            else if (entrySize == null)
                flush = true;
            else if (blockSize + entrySize > desiredBlockSize)
                flush = true;
            else
                flush = false;

            if (flush)
            {
                ranges[blockCount++] = start..end;
                blockSize = 0;
                start = end;
            }

            if (entrySize.HasValue)
                blockSize += entrySize.Value;
        }

        var entryRanges = ImmutableArray.Create(ranges.AsSpan(..blockCount));
        _rangePool.Return(ranges);

        return entryRanges;
    }

    [LoggerMessage(LogLevel.Debug,
    "Total entries: {Count}, record length {RecordLength}")]
    partial void LogInfo(int count, long RecordLength);
}
