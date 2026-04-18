namespace MDictUtils;

public sealed record MDictEntry(string Key, long Pos, string Path, int Size)
{
    public override string ToString()
        => $"Key=\"{Key}\", Pos={Pos}, Size={Size}";
}

#pragma warning disable format
public sealed record MDictMetadata
(
    string Title       = "",
    string Description = "",
    string Version     = "2.0",
    int    KeySize     = 32768,
    int    BlockSize   = 65536
);
#pragma warning restore format

public sealed record MDictWriterOptions
{
    public int CompressionType { get; set; } = 2;
    public bool IsMdd { get; set; } = false;
    public string Encoding { get; set; } = "utf8";
    public bool EnableLogging { get; set; } = true;
}
