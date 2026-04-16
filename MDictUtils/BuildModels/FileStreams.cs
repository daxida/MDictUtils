using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.MemoryMappedFiles;

namespace MDictUtils.BuildModels;

internal sealed class FileStreams(Dictionary<string, int> pathToTotalEntryCount) : IDisposable
{
    private readonly FrozenDictionary<string, int> _pathToTotalEntryCount = pathToTotalEntryCount.ToFrozenDictionary();
    private readonly ConcurrentDictionary<string, int> _pathToEntryCount = [];
    private readonly ConcurrentDictionary<string, MemoryMappedFile> _filepathToFile = [];
    private readonly ConcurrentDictionary<(string Filepath, int ThreadId), MemoryMappedViewStream> _filepathIdToStream = [];
    private readonly List<MemoryMappedFile> _files = [];
    private bool _isDisposed = false;

    public MemoryMappedViewStream GetStream(string filepath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var key = (filepath, Environment.CurrentManagedThreadId);

        return _filepathIdToStream
            .GetOrAdd(key, InitializeStream);
    }

    public void UpdateEntryCount(string filepath)
    {
        var count = _pathToEntryCount.AddOrUpdate
        (
            key: filepath,
            addValue: 1,
            updateValueFactory: static (key, current) => current + 1
        );
        if (count == _pathToTotalEntryCount[filepath])
        {
            DisposePath(filepath);
        }
    }

    private MemoryMappedViewStream InitializeStream((string, int) key)
    {
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

    private void DisposePath(string filepath)
    {
        foreach (var (key, stream) in _filepathIdToStream)
        {
            if (filepath.Equals(key.Filepath, StringComparison.Ordinal))
            {
                stream.Dispose();
            }
        }
        var file = _filepathToFile[filepath];
        file.Dispose();
    }

    void IDisposable.Dispose()
    {
        if (_isDisposed)
            return;

        foreach (var stream in _filepathIdToStream.Values)
            stream.Dispose();
        foreach (var file in _files)
            file.Dispose();

        _isDisposed = true;
    }
}
