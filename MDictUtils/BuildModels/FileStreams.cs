using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace MDictUtils.BuildModels;

internal sealed class FileStreams(int maxOpenStreams = 128) : IDisposable
{
    private readonly int _maxOpenStreams = maxOpenStreams;
    private readonly ConcurrentDictionary<string, MemoryMappedFile> _filepathToFile = [];
    private readonly ConcurrentDictionary<(string, int), MemoryMappedViewStream> _filepathIdToStream = [];
    private readonly List<MemoryMappedFile> _files = [];
    private bool _isDisposed = false;

    public MemoryMappedViewStream GetStream(string filepath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var key = (filepath, Environment.CurrentManagedThreadId);

        return _filepathIdToStream
            .GetOrAdd(key, InitializeStream);
    }

    private MemoryMappedViewStream InitializeStream((string, int) key)
    {
        // Debug.Assert(!_filepathToStream.ContainsKey(filepath));
        // Debug.Assert(_filepathToStream.Count == _files.Count);

        // Sanity check. Please don't use this many files.
        // if (_files.Count >= _maxOpenStreams)
        //     DisposeStreams();

        var file = _filepathToFile.GetOrAdd(key.Item1, InitializeFile);
        return file.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
    }

    private MemoryMappedFile InitializeFile(string filepath)
    {
        var file = MemoryMappedFile
            .CreateFromFile(filepath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _files.Add(file);
        return file;
    }

    private void DisposeStreams()
    {
        foreach (var stream in _filepathToFile.Values)
            stream.Dispose();
        foreach (var file in _files)
            file.Dispose();

        _filepathToFile.Clear();
        _files.Clear();
    }

    void IDisposable.Dispose()
    {
        if (_isDisposed)
            return;

        DisposeStreams();
        _isDisposed = true;
    }
}
