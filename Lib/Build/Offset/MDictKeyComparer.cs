using System.Text.RegularExpressions;

namespace Lib.Build.Offset;

internal abstract partial class MDictKeyComparer : IKeyComparer
{
    /// <summary>
    /// https://docs.python.org/3/library/string.html#string.punctuation
    /// </summary>
    public const string PunctuationChars = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

    /// <summary>
    /// Regex to strip the python punctuation characters, and also the space character.
    /// </summary>
    [GeneratedRegex(@"[!\""#$%&'()*+,\-./:;<=>?@\[\\\]^_`{|}~ ]+")]
    public static partial Regex RegexStrip { get; }

    public abstract int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2);
}

internal class MdxKeyComparer : MDictKeyComparer
{
    public override int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2)
    {
        if (RegexStrip.IsMatch(k1))
            k1 = StripPunctuation(k1);

        if (RegexStrip.IsMatch(k2))
            k2 = StripPunctuation(k2);

        int cmp = k1.CompareTo(k2, StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
            return cmp;

        // reverse length (longer first) - compare on current k1/k2
        if (k1.Length != k2.Length)
            return k2.Length.CompareTo(k1.Length);

        return k2.CompareTo(k1, StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlySpan<char> StripPunctuation(ReadOnlySpan<char> text)
    {
        Span<char> buffer = new char[text.Length];

        int lastIndex = 0;
        int charsWritten = 0;

        foreach (var match in RegexStrip.EnumerateMatches(text))
        {
            text[lastIndex..match.Index].CopyTo(buffer[charsWritten..]);
            charsWritten += match.Index - lastIndex;
            lastIndex = match.Index + match.Length;
        }

        text[lastIndex..text.Length].CopyTo(buffer[charsWritten..]);
        charsWritten += text.Length - lastIndex;

        return buffer[..charsWritten];
    }
}

internal class MddKeyComparer : MDictKeyComparer
{
    public override int Compare(ReadOnlySpan<char> k1, ReadOnlySpan<char> k2)
    {
        int cmp = k1.CompareTo(k2, StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
            return cmp;

        // reverse length (longer first) - compare on current k1/k2
        if (k1.Length != k2.Length)
            return k2.Length.CompareTo(k1.Length);

        // trim punctuation
        k1 = k1.TrimEnd(PunctuationChars);
        k2 = k2.TrimEnd(PunctuationChars);

        return k2.CompareTo(k1, StringComparison.OrdinalIgnoreCase);
    }
}
