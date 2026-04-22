using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MDictUtils.Creation;

public sealed class MdxCreator : MDictCreator
{
    private readonly StreamWriter _txtWriter;

    public MdxCreator(string? filepath = null) : base(filepath)
    {
        _txtWriter = new StreamWriter(_stream, new UTF8Encoding(false)); // No BOM
    }

    public async Task AddEntryAsync(string key, ReadOnlyMemory<char> body)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = Encoding.UTF8.GetByteCount(body.Span);
        _entries.Add(new(key, _filepath, _currentPosition, size + 1)); // Add one extra byte for the null-terminator
        _currentPosition += size;
        await _txtWriter.WriteAsync(body);
    }

    public async Task WriteAsync(MdxHeader header, string outputFile, Action<MdxWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var mdxWriter = GetWriter(configure);
        await _txtWriter.FlushAsync();
        await mdxWriter.WriteAsync(header, _entries, outputFile);
    }

    private static IMdxWriter GetWriter(Action<MdxWriterOptions>? configure)
        => new ServiceCollection()
            .AddMdxWriter(configure)
            .BuildServiceProvider()
            .GetRequiredService<IMdxWriter>();

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
            _txtWriter.Dispose();
        base.Dispose(disposing);
    }
}
