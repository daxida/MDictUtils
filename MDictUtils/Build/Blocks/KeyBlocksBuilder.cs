using System.Threading.Channels;
using MDictUtils.BuildModels;
using Microsoft.Extensions.Logging;

namespace MDictUtils.Build.Blocks;

internal sealed class KeyBlocksBuilder
(
    ILogger<KeyBlocksBuilder> logger,
    IBlockCompressor blockCompressor
)
    : BlocksBuilder<KeyBlock>(logger, blockCompressor)
{
    public ImmutableArray<KeyBlock> Build(OffsetTable offsetTable)
    {
        var blockCount = offsetTable.KeyBlockRanges.Length;
        var blocks = new KeyBlock[blockCount];
        var channel = Channel.CreateUnbounded<(int, KeyBlock)>();

        var readTask = ReadKeyBlocksAsync(blocks, channel);
        var writeTask = WriteBlocksAsync(offsetTable, channel);

        Task.WaitAll(readTask, writeTask);

        return ImmutableArray.Create(blocks);
    }

    private async Task ReadKeyBlocksAsync(KeyBlock[] blocks, Channel<(int Order, KeyBlock Block)> channel)
    {
        await foreach (var orderedBlock in channel.Reader.ReadAllAsync())
        {
            blocks[orderedBlock.Order] = orderedBlock.Block;
        }
    }

    protected override KeyBlock BlockConstructor(ReadOnlySpan<OffsetTableEntry> entries)
    {
        var block = GetCompressedBlock(entries);
        return new(block, entries);
    }

    protected override int GetByteCount(OffsetTableEntry entry)
        => entry.KeyDataSize;

    protected override void WriteBytes(OffsetTableEntry entry, Span<byte> buffer)
    {
        Common.ToBigEndian((ulong)entry.Offset, buffer[..8]);
        entry.NullTerminatedKeyBytes.CopyTo(buffer[8..]);
    }

    protected override ImmutableArray<Range> GetBlockRanges(OffsetTable offsetTable)
        => offsetTable.KeyBlockRanges;
}
