using System.Diagnostics;
using System.Text;
using MDictUtils.Build;
using MDictUtils.Build.Blocks;
using MDictUtils.Build.Compression;
using MDictUtils.Build.Index;
using MDictUtils.Build.Offset;
using MDictUtils.BuildModels;
using MDictUtils.Write;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MDictUtils;

public static class MDictWriterProvider
{
    public static IMDictWriter GetWriter(Action<MDictWriterOptions>? configure = null)
    {
        var options = new MDictWriterOptions();

        if (configure is not null)
            configure(options);

        var s = new ServiceCollection();

        #region Writer services

        s.AddTransient<IMDictWriter, Writer>();
        if (options.IsMdd)
            s.AddTransient<HeaderWriter, MddHeaderWriter>();
        else
            s.AddTransient<HeaderWriter, MdxHeaderWriter>();
        s.AddTransient<KeysWriter>();
        s.AddTransient<RecordsWriter>();

        #endregion

        #region Builder services

        s.AddTransient<IDataBuilder, DataBuilder>();

        // Offset table
        s.AddTransient<OffsetTableBuilder>();
        if (options.IsMdd)
            s.AddTransient<IKeyComparer, MddKeyComparer>();
        else
            s.AddTransient<IKeyComparer, MdxKeyComparer>();
        s.AddTransient(_ => GetEncodingSettings(options.Encoding, options.IsMdd));

        // Key blocks
        s.AddTransient<KeyBlockIndexBuilder>();
        s.AddTransient<KeyBlocksBuilder>();

        // Record blocks
        s.AddTransient<RecordBlockIndexBuilder>();
        if (options.IsMdd)
            s.AddTransient<IRecordBlocksBuilder, MddRecordBlocksBuilder>();
        else
            s.AddTransient<IRecordBlocksBuilder, MdxRecordBlocksBuilder>();

        // Compression
        if (options.CompressionType == ZLibBlockCompressor.CompressionType)
            s.AddTransient<IBlockCompressor, ZLibBlockCompressor>();
        else
            throw new NotSupportedException($"Unsupported compression type `{options.CompressionType}`");

        #endregion

        // Logging
        s.AddLogging(builder =>
        {
            if (options.EnableLogging)
                builder.SetMinimumLevel(LogLevel.Debug);

            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = false;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        // Build and return the writer service.
        var provider = s.BuildServiceProvider();
        return provider.GetRequiredService<IMDictWriter>();
    }

    private static EncodingSettings GetEncodingSettings(string encoding, bool isMdd)
    {
        encoding = encoding.ToLower();
        Debug.Assert(encoding == "utf8");

        if (isMdd || encoding == "utf16" || encoding == "utf-16")
        {
            return new(Encoding.Unicode, EncodingLength: 2);
        }
        else if (encoding == "utf8" || encoding == "utf-8")
        {
            return new(Encoding.UTF8, EncodingLength: 1);
        }
        else
        {
            throw new NotSupportedException("Unknown encoding. Supported: utf8, utf16");
        }
    }
}
