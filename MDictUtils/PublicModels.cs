namespace MDictUtils;

public sealed record MDictEntry(string Key, long Pos, string Path, int Size)
{
    public override string ToString()
        => $"Key=\"{Key}\", Pos={Pos}, Size={Size}";
}

public abstract record MDictHeader
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Version { get; init; } = "2.0";

    // Same as python: escape(self._description, quote=True),
    // System.Web.HttpUtility.HtmlAttributeEncode(s) doesn't do the trick...
    protected static string EscapeHtml(string s)
    {
        return s
            .Replace("&", "&amp;")   // Must be first
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }
}

public sealed record MDictWriterOptions
{
    public int CompressionType { get; set; } = 2;
    public int DesiredRecordBlockSize { get; set; } = 65_536;
    public int DesiredKeyBlockSize { get; set; } = 32_768;
    public bool EnableLogging { get; set; } = true;
    public string Encoding { get; set; } = "utf8";
    public bool IsMdd { get; set; } = false;
}
