using MDictUtils.Build;

namespace MDictUtils.Write;

internal sealed class Writer
(
    IDataBuilder dataBuilder,
    HeaderWriter headerWriter,
    KeysWriter keysWriter,
    RecordsWriter recordsWriter
)
    : IMDictWriter
{
    public void Write(List<MDictEntry> entries, string outputFile, MDictHeader header)
    {
        if (header.Version != "2.0")
            throw new NotSupportedException("Unknown version. Supported: 2.0");

        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);

        int bytesWritten = headerWriter.Write(stream, header);

        var keyData = dataBuilder.BuildKeyData(entries);
        bytesWritten += keysWriter.Write(stream, keyData);

        var recordData = dataBuilder.BuildRecordData(entries);
        recordsWriter.Write(stream, recordData);
    }
}
