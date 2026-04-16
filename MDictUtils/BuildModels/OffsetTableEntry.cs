using System.Text;

namespace MDictUtils.BuildModels;

internal class OffsetTableEntry
{
    public required ImmutableArray<byte> KeyNull { get; init; }
    public required int KeyLen { get; init; }
    public required long Offset { get; init; }
    public required long RecordSize { get; init; }
    public required long RecordPos { get; init; }
    public required string FilePath { get; init; }

    public long KeyBlockLength => 8 + KeyNull.Length;
    public long RecordBlockLength => RecordSize;

    public override string ToString()
    {
        static string BytesToString(ReadOnlySpan<byte> bytes)
            => bytes.IsEmpty ? "null" : Encoding.UTF8.GetString(bytes);

        var sb = new StringBuilder();
        sb.Append("OffsetTableEntry(");
        sb.Append($"KeyLen={KeyLen}, ");
        sb.Append($"Offset={Offset}, ");
        sb.Append($"RecordPos={RecordPos}, ");
        sb.Append($"RecordSize={RecordSize}, ");
        sb.Append($"KeyNull='{BytesToString(KeyNull.AsSpan())}', ");
        sb.Append(')');
        return sb.ToString();
    }
}
