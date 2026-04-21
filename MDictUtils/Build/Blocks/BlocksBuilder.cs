using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;
using MDictUtils.BuildModels;
using MDictUtils.Extensions;
using Microsoft.Extensions.Logging;

namespace MDictUtils.Build.Blocks;

internal abstract partial class BlocksBuilder<T>
(
    ILogger<BlocksBuilder<T>> logger,
    IBlockCompressor blockCompressor
)
    where T : MDictBlock
{
    private static readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    private static readonly string _typeName = typeof(T).Name;

    protected abstract Task<T> BlockConstructorAsync(int id, ReadOnlyMemory<OffsetTableEntry> entries);
    protected abstract int GetByteCount(OffsetTableEntry entry);
    protected abstract void WriteBytes(OffsetTableEntry entry, Span<byte> buffer);
    protected abstract ImmutableArray<Range> GetBlockRanges(OffsetTable offsetTable);

    protected async Task BuildBlocksAsync(OffsetTable offsetTable, ChannelWriter<T> channel)
    {
        LogBeginBuilding(_typeName);
        var blockRanges = GetBlockRanges(offsetTable);

        await Parallel.ForAsync(0, blockRanges.Length, async (i, ct) =>
        {
            var blockRange = blockRanges[i];
            var entries = offsetTable.AsMemory(blockRange);
            var block = await BlockConstructorAsync(i, entries);
            await channel.WriteAsync(block, ct);
        });

        channel.Complete();
    }

    protected async Task<CompressedBlock> GetCompressedBlockAsync(ReadOnlyMemory<OffsetTableEntry> entries)
    {
        int totalSize = entries.Span.Sum(GetByteCount);
        var uncompressed = _memoryPool.Rent(totalSize);

        int position = 0;
        foreach (var entry in entries.Span)
        {
            var size = GetByteCount(entry);
            var buffer = uncompressed.Memory.Slice(start: position, size).Span;
            WriteBytes(entry, buffer);
            position += size;
        }

        var compressed = await blockCompressor
            .CompressAsync(uncompressed.Memory[..position]);

        uncompressed.Dispose();
        Debug.Assert(totalSize == position);

        return new(compressed, DecompSize: position);
    }

    [LoggerMessage(LogLevel.Debug, "Building blocks of type {Type}")]
    private partial void LogBeginBuilding(string type);
}
