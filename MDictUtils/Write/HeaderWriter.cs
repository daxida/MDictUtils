using System.Text;

namespace MDictUtils.Write;

internal sealed class HeaderWriter
{
    public int WriteHeader(Stream stream, MDictHeader fields)
    {
        var header = fields.ToString();

        // Encode header to little-endian UTF-16
        ReadOnlySpan<byte> headerBytes = Encoding.Unicode.GetBytes(header);

        // Write header length (big-endian)
        Span<byte> lengthBytes = stackalloc byte[4];
        Common.ToBigEndian((uint)headerBytes.Length, lengthBytes);
        stream.Write(lengthBytes);

        // Write header string
        stream.Write(headerBytes);

        // Write Adler32 checksum (little-endian)
        uint checksum = Common.Adler32(headerBytes);
        Span<byte> checksumBytes = stackalloc byte[4];
        Common.ToLittleEndian(checksum, checksumBytes);

        stream.Write(checksumBytes);

        return lengthBytes.Length
            + headerBytes.Length
            + checksumBytes.Length;
    }
}
