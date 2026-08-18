using System.Text;

namespace OpenLearning.Web.Pages.Admin.Reports;

/// <summary>
/// Minimal CSV writer that quotes cells containing separators, quotes, or
/// newlines, doubles embedded quotes, and neutralizes spreadsheet-formula
/// injection (cells starting with =, +, -, @ are prefixed with a tab).
/// </summary>
public static class CsvHelper
{
    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && (text[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
        {
            text = "'" + text;
        }

        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }

    public static string Build(string[] header, IEnumerable<string?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", header.Select(Escape)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        }
        return sb.ToString();
    }
}
