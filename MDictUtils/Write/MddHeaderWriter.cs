using System.Text;
using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal sealed class MddHeaderWriter : HeaderWriter
{
    protected internal override string GetHeaderString(HeaderFields fields)
    {
        var now = DateTime.Today;
        var sb = new StringBuilder();

        void append(ReadOnlySpan<char> val)
        {
            sb.Append(val.Trim());
            sb.Append(' ');
        }

        append($"""  <Library_Data                                    """);
        append($"""  GeneratedByEngineVersion="{fields.Version}"      """);
        append($"""  RequiredEngineVersion="{fields.Version}"         """);
        append($"""  Encrypted="No"                                   """);
        append($"""  Encoding=""                                      """);
        append($"""  Format=""                                        """);
        append($"""  CreationDate="{now.Year}-{now.Month}-{now.Day}"  """);
        append($"""  KeyCaseSensitive="No"                            """);
        append($"""  Stripkey="No"                                    """);
        append($"""  Description="{EscapeHtml(fields.Description)}"   """);
        append($"""  Title="{EscapeHtml(fields.Title)}"               """);
        append($"""  RegisterBy=""                                    """);

        sb.Append("/>\r\n\0");
        return sb.ToString();
    }
}
