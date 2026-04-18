using System.Text;

namespace MDictUtils.Write;

public sealed record MddHeader : MDictHeader
{
    public override string ToString()
    {
        var now = DateTime.Today;
        var sb = new StringBuilder();

        void append(ReadOnlySpan<char> val)
        {
            sb.Append(val.Trim());
            sb.Append(' ');
        }

        append($"""  <Library_Data                                    """);
        append($"""  GeneratedByEngineVersion="{Version}"             """);
        append($"""  RequiredEngineVersion="{Version}"                """);
        append($"""  Encrypted="No"                                   """);
        append($"""  Encoding=""                                      """);
        append($"""  Format=""                                        """);
        append($"""  CreationDate="{now.Year}-{now.Month}-{now.Day}"  """);
        append($"""  KeyCaseSensitive="No"                            """);
        append($"""  Stripkey="No"                                    """);
        append($"""  Description="{EscapeHtml(Description)}"          """);
        append($"""  Title="{EscapeHtml(Title)}"                      """);
        append($"""  RegisterBy=""                                    """);

        sb.Append("/>\r\n\0");
        return sb.ToString();
    }
}
