using System.Buffers;
using System.Text;
using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal sealed class HeaderWriter(BuildOptions options)
{
    private static readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;

    public async Task WriteAsync(Stream stream, MDictHeader header)
    {
        header = FixHeaderEncoding(header);

        var xmlString = header.ToString();
        var xmlSize = Encoding.Unicode.GetByteCount(xmlString);
        var headerSize = xmlSize + 8;

        using var memoryOwner = _memoryPool.Rent(headerSize);

        var headerBytes = memoryOwner.Memory[..headerSize];

        var sizeBytes = headerBytes.Span[..4];
        var xmlBytes = headerBytes.Span[4..^4];
        var checksumBytes = headerBytes.Span[^4..headerSize];

        // Size
        Common.ToBigEndian((uint)xmlSize, sizeBytes);

        // XML
        Encoding.Unicode.GetBytes(xmlString, xmlBytes);

        // Checksum
        uint checksum = Common.Adler32(xmlBytes);
        Common.ToLittleEndian(checksum, checksumBytes);

        // Output
        await stream.WriteAsync(headerBytes);
    }

    /// <summary>
    /// Sets the header encoding to the key encoding if the user did not set it manually.
    /// </summary>
    private MDictHeader FixHeaderEncoding(MDictHeader header)
    {
        if (header is MdxHeader mdxHeader && mdxHeader.Encoding is null)
        {
            header = mdxHeader with
            {
                Encoding = options.KeyEncoding switch
                {
                    UTF8Encoding => "UTF-8",
                    UnicodeEncoding => "UTF-16",
                    _ => null
                }
            };
        }
        return header;
    }
}
