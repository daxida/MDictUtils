namespace MDictUtils;

public sealed record MDictWriterOptions
{
    public uint CompressionType { get; set; } = 2;
    public int DesiredKeyBlockSize { get; set; } = 32_768;
    public int DesiredRecordBlockSize { get; set; } = 65_536;
    public bool EnableLogging { get; set; } = true;
    public string Encoding { get; set; } = "utf8";
    public bool IsMdd { get; set; } = false;
}
