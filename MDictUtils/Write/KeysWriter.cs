using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal sealed class KeysWriter
{
    public void Write(Stream outfile, KeyData data, int entryCount)
    {
        Span<byte> preamble = stackalloc byte[5 * 8]; // Five 8-byte buffers
        var r = new SpanReader<byte>(preamble) { ReadSize = 8 };

        Common.ToBigEndian((ulong)data.KeyBlocks.Length, r.Read());
        Common.ToBigEndian((ulong)entryCount, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlockIndex.DecompSize, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlockIndex.Size, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlocksSize, r.Read());

        uint checksumValue = Common.Adler32(preamble);
        Span<byte> checksum = stackalloc byte[4];
        Common.ToBigEndian(checksumValue, checksum);

        outfile.Write(preamble);
        outfile.Write(checksum);
        outfile.Write(data.KeyBlockIndex.Bytes.AsSpan());

        foreach (var block in data.KeyBlocks)
        {
            outfile.Write(block.Bytes.AsSpan());
        }
    }
}
