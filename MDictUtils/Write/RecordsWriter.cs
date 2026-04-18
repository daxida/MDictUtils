using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal sealed class RecordsWriter
{
    public void Write(Stream outfile, RecordData data)
    {
        Span<byte> preamble = stackalloc byte[4 * 8]; // Four 8-byte buffers
        var r = new SpanReader<byte>(preamble) { ReadSize = 8 };

        Common.ToBigEndian((ulong)data.RecordBlocks.Length, r.Read());
        Common.ToBigEndian((ulong)data.EntryCount, r.Read());
        Common.ToBigEndian((ulong)data.RecordBlockIndex.Size, r.Read());
        Common.ToBigEndian((ulong)data.RecordBlocksSize, r.Read());

        outfile.Write(preamble);
        outfile.Write(data.RecordBlockIndex.Bytes.AsSpan());

        foreach (var block in data.RecordBlocks)
        {
            outfile.Write(block.Bytes.AsSpan());
        }
    }
}
