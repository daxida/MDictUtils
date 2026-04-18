using MDictUtils.Build;
using MDictUtils.BuildModels;

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
    public void Write(List<MDictEntry> entries, string outputFile, MDictMetadata? metadata = null)
    {
        metadata ??= new();

        if (metadata.Version != "2.0")
            throw new NotSupportedException("Unknown version. Supported: 2.0");

        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);

        int bytesWritten = headerWriter.WriteHeader(stream, metadata);

        var keyData = dataBuilder.BuildKeyData(entries, metadata);
        bytesWritten += keysWriter.Write(stream, keyData);

        var recordData = dataBuilder.BuildRecordData(entries, metadata);
        recordsWriter.Write(stream, recordData);
    }
}
