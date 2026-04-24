using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MDictUtils.Creation;

public sealed class MdxCreator : MDictCreator
{
    private readonly Encoding _encoding;
    private readonly int _nullLength;

    /// <summary>
    /// Class for building new MDX files.
    /// </summary>
    /// <param name="filepath">
    /// Path to temporary file for storing entry records. Overwritten if exists.
    /// </param>
    /// <param name="encoding">
    /// Encoding for the text of entry records.
    /// Note that the encoding for keys configured by <see cref="MdxWriterOptions"/>.
    /// </param>
    public MdxCreator(string? filepath = null, Encoding? encoding = null) : base(filepath)
    {
        _encoding = encoding ?? Encoding.UTF8;
        _nullLength = _encoding.GetByteCount("\0");
    }

    public async Task AddEntryAsync(string key, string record)
    {
        var bytes = _encoding.GetBytes(record);
        await AddEntryAsync(key, bytes);
    }

    public async Task AddEntryAsync(string key, ReadOnlyMemory<byte> record)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = record.Length;
        _entries.Add(new(key, _filepath, _currentPosition, size + _nullLength));
        _currentPosition += size;
        await _stream.WriteAsync(record);
    }

    public async Task WriteAsync(MdxHeader header, string outputFile, Action<MdxWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var mdxWriter = GetWriter(configure);
        await _stream.FlushAsync();
        await mdxWriter.WriteAsync(header, _entries, outputFile);
    }

    private static IMdxWriter GetWriter(Action<MdxWriterOptions>? configure)
        => new ServiceCollection()
            .AddMdxWriter(configure)
            .BuildServiceProvider()
            .GetRequiredService<IMdxWriter>();
}
