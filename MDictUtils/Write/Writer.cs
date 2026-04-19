using System.Threading.Channels;
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
    public void Write(MDictHeader header, List<MDictEntry> entries, string outputFile)
    {
        if (header.Version != "2.0")
            throw new NotSupportedException("Unknown version. Supported: 2.0");

        using var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        headerWriter.Write(stream, header);

        var offsetTable = dataBuilder.BuildOffsetTable(entries);

        // Process key data.
        var keyData = dataBuilder.BuildKeyData(offsetTable);
        var entryCount = offsetTable.Length;
        keysWriter.Write(stream, keyData, entryCount);

        // Concurrently read, compress, and write record data to the disk.
        // This is where the heavy lifting happens.
        var channel = GetRecordBlockChannel();
        var buildTask = dataBuilder.BuildRecordBlocksAsync(offsetTable, channel);
        var writeTask = recordsWriter.WriteAsync(offsetTable, channel, stream);
        Task.WaitAll(buildTask, writeTask);
    }

    private static Channel<(int, RecordBlock)> GetRecordBlockChannel()
    {
        var option = new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        return Channel.CreateBounded<(int, RecordBlock)>(option);
    }
}
