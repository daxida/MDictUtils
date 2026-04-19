using System.Threading.Channels;
using MDictUtils.BuildModels;
using Microsoft.Extensions.Logging;
using OrderedBlock = (int Order, MDictUtils.BuildModels.KeyBlock Block);

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
        var channel = Channel.CreateUnbounded<OrderedBlock>();

        var readTask = ReadKeyBlocksAsync(blocks, channel);
        var buildTask = BuildBlocksAsync(offsetTable, channel);

        Task.WaitAll(readTask, buildTask);

        return ImmutableArray.Create(blocks);
    }

    private async Task ReadKeyBlocksAsync(KeyBlock[] blocks, Channel<OrderedBlock> channel)
    {
        await foreach (var (i, block) in channel.Reader.ReadAllAsync())
        {
            blocks[i] = block;
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
