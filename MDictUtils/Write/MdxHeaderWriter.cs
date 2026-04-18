using System.Text;
using MDictUtils.BuildModels;

namespace MDictUtils.Write;

internal sealed class MdxHeaderWriter : HeaderWriter
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

        append($"""  <Dictionary                                      """);
        append($"""  GeneratedByEngineVersion="{fields.Version}"      """);
        append($"""  RequiredEngineVersion="{fields.Version}"         """);
        append($"""  Encrypted="No"                                   """);
        append($"""  Encoding="UTF-8"                                 """);
        append($"""  Format="Html"                                    """);
        append($"""  Stripkey="Yes"                                   """);
        append($"""  CreationDate="{now.Year}-{now.Month}-{now.Day}"  """);
        append($"""  Compact="Yes"                                    """);
        append($"""  Compat="Yes"                                     """);
        append($"""  KeyCaseSensitive="No"                            """);
        append($"""  Description="{EscapeHtml(fields.Description)}"   """);
        append($"""  Title="{EscapeHtml(fields.Title)}"               """);
        append($"""  DataSourceFormat="106"                           """);
        append($"""  StyleSheet=""                                    """);
        append($"""  Left2Right="Yes"                                 """);
        append($"""  RegisterBy=""                                    """);

        sb.Append("/>\r\n\0");
        return sb.ToString();
    }
}
