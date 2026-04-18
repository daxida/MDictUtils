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

public interface IMDictWriter
{
    public void Write(List<MDictEntry> entries, string outputFile, MDictHeader header);
}

public sealed record MDictWriterOptions
{
    public uint CompressionType { get; set; } = 2;
    public int DesiredKeyBlockSize { get; set; } = 32_768;
    public int DesiredRecordBlockSize { get; set; } = 65_536;
    public bool EnableLogging { get; set; } = true;
    public string Encoding { get; set; } = "utf8";
    public bool IsMdd { get; set; } = false;
}

public static class MDictWriterProvider
{
    public static IMDictWriter GetWriter(Action<MDictWriterOptions>? configure = null)
    {
        var options = new MDictWriterOptions();

        if (configure is not null)
            configure(options);

        var s = new ServiceCollection();

        /// Inject <see cref="Write"> namespace types.
        s.AddWriterServices();

        /// Inject <see cref="Build"> namespace types.
        if (options.IsMdd)
            s.AddMddBuilderServices(options.CompressionType);
        else
            s.AddMdxBuilderServices(options.CompressionType);

        // Inject option wrapper objects.
        s.AddTransient(_ => new DesiredKeyBlockSize(options.DesiredKeyBlockSize));
        s.AddTransient(_ => new DesiredRecordBlockSize(options.DesiredRecordBlockSize));
        s.AddTransient(_ => new EncodingSettings(options.Encoding, options.IsMdd));

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

    private static IServiceCollection AddWriterServices(this IServiceCollection services)
        => services
            .AddTransient<IMDictWriter, Writer>()
            .AddTransient<HeaderWriter>()
            .AddTransient<KeysWriter>()
            .AddTransient<RecordsWriter>();

    private static IServiceCollection AddMdxBuilderServices(this IServiceCollection services, uint compressionType)
        => services
            .AddTransient<IDataBuilder, DataBuilder>()
            .AddTransient<IKeyComparer, MdxKeyComparer>()
            .AddTransient<OffsetTableBuilder>()
            .AddTransient<KeyBlockIndexBuilder>()
            .AddTransient<KeyBlocksBuilder>()
            .AddTransient<RecordBlockIndexBuilder>()
            .AddTransient<IRecordBlocksBuilder, MdxRecordBlocksBuilder>()
            .AddBlockCompressor(compressionType);

    private static IServiceCollection AddMddBuilderServices(this IServiceCollection services, uint compressionType)
        => services
            .AddTransient<IDataBuilder, DataBuilder>()
            .AddTransient<IKeyComparer, MddKeyComparer>()
            .AddTransient<OffsetTableBuilder>()
            .AddTransient<KeyBlockIndexBuilder>()
            .AddTransient<KeyBlocksBuilder>()
            .AddTransient<RecordBlockIndexBuilder>()
            .AddTransient<IRecordBlocksBuilder, MddRecordBlocksBuilder>()
            .AddBlockCompressor(compressionType);

    private static IServiceCollection AddBlockCompressor(this IServiceCollection services, uint compressionType)
        => compressionType switch
        {
            ZLibBlockCompressor.CompressionType
                => services.AddTransient<IBlockCompressor, ZLibBlockCompressor>(),
            _ // Default
                => throw new NotSupportedException($"Unsupported compression type `{compressionType}`")
        };
}
