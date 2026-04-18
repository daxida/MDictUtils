namespace MDictUtils;

public sealed record MDictEntry
(
    string Key,
    long Pos,
    string Path,
    int Size
)
{
    public override string ToString()
        => $"Key=\"{Key}\", Pos={Pos}, Size={Size}";
}
