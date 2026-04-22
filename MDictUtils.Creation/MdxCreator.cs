using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MDictUtils.Creation;

public sealed class MdxCreator : MDictCreator
{
    private readonly StreamWriter _writer;

    public MdxCreator()
    {
        _writer = new StreamWriter(_stream, new UTF8Encoding(false)); // No BOM
    }

    public void AddEntry(string key, ReadOnlySpan<char> body)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = Encoding.UTF8.GetByteCount(body);
        _entries.Add(new(key, _filepath, _currentPosition, size + 1)); // Add one extra byte for the null-terminator
        _currentPosition += size;
        _writer.Write(body);
    }

    public async Task WriteAsync(MdxHeader header, string outputFile, Action<MdxWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var writer = new ServiceCollection()
            .AddMdxWriter(configure)
            .BuildServiceProvider()
            .GetRequiredService<IMdxWriter>();

        await _writer.FlushAsync();
        await writer.WriteAsync(header, _entries, outputFile);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
            _writer.Dispose();
        base.Dispose(disposing);
    }
}
