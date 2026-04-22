using Microsoft.Extensions.DependencyInjection;

namespace MDictUtils.Creation;

public sealed class MddCreator(string? filepath = null) : MDictCreator(filepath)
{
    public void AddEntry(string key, ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = bytes.Length;
        _entries.Add(new(key, _filepath, _currentPosition, size));
        _currentPosition += size;
        _stream.Write(bytes);
    }

    public async Task AddEntryAsync(string key, ReadOnlyMemory<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = bytes.Length;
        _entries.Add(new(key, _filepath, _currentPosition, size));
        _currentPosition += size;
        await _stream.WriteAsync(bytes);
    }

    public void Write(MddHeader header, string outputFile, Action<MddWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var writer = GetWriter(configure);
        _stream.Flush();
        writer.WriteAsync(header, _entries, outputFile).GetAwaiter().GetResult();
    }

    public async Task WriteAsync(MddHeader header, string outputFile, Action<MddWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var writer = GetWriter(configure);
        await _stream.FlushAsync();
        await writer.WriteAsync(header, _entries, outputFile);
    }

    private static IMddWriter GetWriter(Action<MddWriterOptions>? configure)
        => new ServiceCollection()
            .AddMddWriter(configure)
            .BuildServiceProvider()
            .GetRequiredService<IMddWriter>();
}
