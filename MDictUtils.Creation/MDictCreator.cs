namespace MDictUtils.Creation;

public abstract class MDictCreator : IDisposable
{
    protected bool _isDisposed;
    protected readonly List<MDictEntry> _entries = [];
    protected readonly string _filepath;
    protected readonly Stream _stream;
    protected long _currentPosition = 0;

    protected MDictCreator()
    {
        _filepath = Path.GetTempFileName();
        _stream = new FileStream(_filepath, FileMode.Open, FileAccess.Write, FileShare.Read);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
                _stream.Dispose();
            if (File.Exists(_filepath))
                File.Delete(_filepath);
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
