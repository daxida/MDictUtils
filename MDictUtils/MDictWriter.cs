using System.Text;
using MDictUtils.Build;
using MDictUtils.BuildModels;

namespace MDictUtils;

public sealed record MDictEntry(string Key, long Pos, string Path, int Size)
{
    public override string ToString()
        => $"Key=\"{Key}\", Pos={Pos}, Size={Size}";
}

#pragma warning disable format
public sealed record MDictMetadata
(
    string Title       = "",
    string Description = "",
    string Version     = "2.0",
    int    KeySize     = 32768,
    int    BlockSize   = 65536
);
#pragma warning restore format

internal sealed class Writer
(
    IDataBuilder dataBuilder,
    HeaderWriter headerWriter,
    KeysWriter keysWriter,
    RecordsWriter recordsWriter
)
    : IMDictWriter
{
    public void Write(List<MDictEntry> entries, string outputFile, MDictMetadata? metadata = null)
    {
        metadata ??= new();

        if (metadata.Version != "2.0")
            throw new NotSupportedException("Unknown version. Supported: 2.0");

        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);

        var headerFields = new HeaderFields(metadata.Version, metadata.Title, metadata.Description);
        int bytesWritten = headerWriter.WriteHeader(stream, headerFields);

        var keyData = dataBuilder.BuildKeyData(entries, metadata);
        bytesWritten += keysWriter.Write(stream, keyData);

        var recordData = dataBuilder.BuildRecordData(entries, metadata);
        recordsWriter.Write(stream, recordData);
    }
}

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

internal sealed class MddHeaderWriter : HeaderWriter
{
    protected internal override string GetHeaderString(HeaderFields fields)
    {
        var now = DateTime.Today;
        var sb = new StringBuilder();

        void append(ReadOnlySpan<char> val)
        {
            sb.Append(val.Trim());
            sb.Append(' ');
        }

        append($"""  <Library_Data                                    """);
        append($"""  GeneratedByEngineVersion="{fields.Version}"      """);
        append($"""  RequiredEngineVersion="{fields.Version}"         """);
        append($"""  Encrypted="No"                                   """);
        append($"""  Encoding=""                                      """);
        append($"""  Format=""                                        """);
        append($"""  CreationDate="{now.Year}-{now.Month}-{now.Day}"  """);
        append($"""  KeyCaseSensitive="No"                            """);
        append($"""  Stripkey="No"                                    """);
        append($"""  Description="{EscapeHtml(fields.Description)}"   """);
        append($"""  Title="{EscapeHtml(fields.Title)}"               """);
        append($"""  RegisterBy=""                                    """);

        sb.Append("/>\r\n\0");
        return sb.ToString();
    }
}

internal sealed class MdxHeaderWriter : HeaderWriter
{
    protected internal override string GetHeaderString(HeaderFields fields)
    {
        var now = DateTime.Today;
        var sb = new StringBuilder();

        void append(ReadOnlySpan<char> val)
        {
            sb.Append(val.Trim());
            sb.Append(' ');
        }

        append($"""  <Dictionary                                      """);
        append($"""  GeneratedByEngineVersion="{fields.Version}"      """);
        append($"""  RequiredEngineVersion="{fields.Version}"         """);
        append($"""  Encrypted="No"                                   """);
        append($"""  Encoding="UTF-8"                                 """);
        append($"""  Format="Html"                                    """);
        append($"""  Stripkey="Yes"                                   """);
        append($"""  CreationDate="{now.Year}-{now.Month}-{now.Day}"  """);
        append($"""  Compact="Yes"                                    """);
        append($"""  Compat="Yes"                                     """);
        append($"""  KeyCaseSensitive="No"                            """);
        append($"""  Description="{EscapeHtml(fields.Description)}"   """);
        append($"""  Title="{EscapeHtml(fields.Title)}"               """);
        append($"""  DataSourceFormat="106"                           """);
        append($"""  StyleSheet=""                                    """);
        append($"""  Left2Right="Yes"                                 """);
        append($"""  RegisterBy=""                                    """);

        sb.Append("/>\r\n\0");
        return sb.ToString();
    }
}

internal sealed class KeysWriter
{
    public int Write(Stream outfile, KeyData data)
    {
        Span<byte> preamble = stackalloc byte[5 * 8]; // Five 8-byte buffers
        var r = new SpanReader<byte>(preamble) { ReadSize = 8 };

        Common.ToBigEndian((ulong)data.KeyBlocks.Length, r.Read());
        Common.ToBigEndian((ulong)data.EntryCount, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlockIndex.DecompSize, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlockIndex.Size, r.Read());
        Common.ToBigEndian((ulong)data.KeyBlocksSize, r.Read());

        uint checksumValue = Common.Adler32(preamble);
        Span<byte> checksum = stackalloc byte[4];
        Common.ToBigEndian(checksumValue, checksum);

        outfile.Write(preamble);
        outfile.Write(checksum);
        outfile.Write(data.KeyBlockIndex.Bytes.AsSpan());

        var bytesWritten = preamble.Length
            + checksum.Length
            + data.KeyBlockIndex.Bytes.Length;

        foreach (var block in data.KeyBlocks)
        {
            outfile.Write(block.Bytes.AsSpan());
            bytesWritten += block.Bytes.Length;
        }

        return bytesWritten;
    }
}

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
