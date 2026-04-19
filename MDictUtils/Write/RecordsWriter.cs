using System.Threading.Channels;
using MDictUtils.BuildModels;
using OrderedBlock = (int Order, MDictUtils.BuildModels.RecordBlock Block);

namespace MDictUtils.Write;

internal sealed class RecordsWriter
{
    private const int IndexPreambleSize = 4 * 8; // Four 8-byte buffers

    public async Task WriteAsync(OffsetTable offsetTable, Channel<OrderedBlock> channel, Stream outfile)
    {
        var blockCount = offsetTable.RecordBlockRanges.Length;
        var entryCount = offsetTable.Length;

        var indexStartPosition = outfile.Position;
        var indexSize = blockCount * 16;
        var index = new byte[indexSize];

        // Skip over the index sections for now.
        outfile.Seek(IndexPreambleSize + indexSize, SeekOrigin.Current);
        var totalSize = await WriteOutputAsync(outfile, channel, index);

        // Return to the start of the index sections.
        outfile.Seek(indexStartPosition, SeekOrigin.Begin);
        var preamble = GetIndexPreamble(blockCount, entryCount, indexSize, totalSize);
        outfile.Write(preamble);
        outfile.Write(index.AsSpan());
    }

    async Task<long> WriteOutputAsync(Stream outfile, Channel<OrderedBlock> channel, byte[] index)
    {
        long totalSize = 0;
        var blockCount = index.Length / 16;
        var blocks = new RecordBlock?[blockCount];
        int order = 0;

        await foreach (var orderedBlock in channel.Reader.ReadAllAsync())
        {
            blocks[orderedBlock.Order] = orderedBlock.Block;

            // Ensure that blocks are always written in sequential order.
            while (blocks[order] is RecordBlock block) // (not null)
            {
                totalSize += block.Bytes.Length;
                outfile.Write(block.Bytes.AsSpan());

                int start = order * 16;
                block.CopyIndexEntryTo(index.AsSpan(start, 16));

                blocks[order] = null;
                order++;

                if (order == blockCount)
                    break;
            }
        }

        return totalSize;
    }

    private ReadOnlySpan<byte> GetIndexPreamble(int blockCount, int entryCount, int indexSize, long totalSize)
    {
        Span<byte> preamble = new byte[IndexPreambleSize];
        var r = new SpanReader<byte>(preamble) { ReadSize = 8 };

        Common.ToBigEndian((ulong)blockCount, r.Read());
        Common.ToBigEndian((ulong)entryCount, r.Read());
        Common.ToBigEndian((ulong)indexSize, r.Read()); // Redundant? Always equal to blockCount * 16.
        Common.ToBigEndian((ulong)totalSize, r.Read());

        return preamble;
    }
}
