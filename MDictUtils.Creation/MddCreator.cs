using Microsoft.Extensions.DependencyInjection;

namespace MDictUtils.Creation;

public sealed class MddCreator : MDictCreator
{
    public void AddEntry(string key, ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var size = bytes.Length;
        _entries.Add(new(key, _filepath, _currentPosition, size));
        _currentPosition += size;
        _stream.Write(bytes);
    }

    public async Task WriteAsync(MddHeader header, string outputFile, Action<MddWriterOptions>? configure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var writer = new ServiceCollection()
            .AddMddWriter(configure)
            .BuildServiceProvider()
            .GetRequiredService<IMddWriter>();

        await _stream.FlushAsync();
        await writer.WriteAsync(header, _entries, outputFile);
    }
}
