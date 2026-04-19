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
        var keyData = dataBuilder.BuildKeyData(offsetTable);
        keysWriter.Write(stream, keyData);

        var channel = GetRecordBlockChannel();
        var entryCount = offsetTable.Length;
        var recordBlockCount = offsetTable.RecordBlockRanges.Length;

        var readTask = dataBuilder.ReadRecordBlocksAsync(offsetTable, channel);
        var writeTask = recordsWriter.WriteAsync(offsetTable, channel, stream);

        Task.WaitAll(readTask, writeTask);
    }

    private Channel<(int, RecordBlock)> GetRecordBlockChannel()
    {
        var option = new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        return Channel.CreateBounded<(int, RecordBlock)>(option);
    }
}
