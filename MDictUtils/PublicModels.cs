namespace MDictUtils;

public sealed record MDictEntry(string Key, long Pos, string Path, int Size)
{
    public override string ToString()
        => $"Key=\"{Key}\", Pos={Pos}, Size={Size}";
}

public sealed record MDictMetadata
(
    string Title = "",
    string Description = "",
    string Version = "2.0"
);

public sealed record MDictWriterOptions
{
    public int CompressionType { get; set; } = 2;
    public int DesiredRecordBlockSize { get; set; } = 65_536;
    public int DesiredKeyBlockSize { get; set; } = 32_768;
    public bool EnableLogging { get; set; } = true;
    public string Encoding { get; set; } = "utf8";
    public bool IsMdd { get; set; } = false;
}
