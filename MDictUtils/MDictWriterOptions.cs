using System.Text;

namespace MDictUtils;

public sealed record MDictWriterOptions
{
    public MDictCompressionType CompressionType { get; set; } = MDictCompressionType.ZLib;
    public int DesiredKeyBlockSize { get; set; } = 32_768;
    public int DesiredRecordBlockSize { get; set; } = 65_536;
    public bool EnableLogging { get; set; } = true;
    public Encoding KeyEncoding { get; set; } = Encoding.UTF8;
    public bool IsMdd { get; set; } = false;
}

public enum MDictCompressionType : uint
{
    None = 0,
    LZO = 1,
    ZLib = 2,
}
