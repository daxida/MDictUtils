using System.Text;
using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal abstract class HeaderWriter
{
    public int WriteHeader(Stream stream, HeaderFields fields)
    {
        var header = GetHeaderString(fields);

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

    protected internal abstract string GetHeaderString(HeaderFields fields);

    // Same as python: escape(self._description, quote=True),
    // System.Web.HttpUtility.HtmlAttributeEncode(s) doesn't do the trick...
    protected static string EscapeHtml(string s)
    {
        return s
            .Replace("&", "&amp;")   // Must be first
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }
}
