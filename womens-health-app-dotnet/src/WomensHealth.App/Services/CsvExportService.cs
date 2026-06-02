using System.Text;

namespace WomensHealth.App.Services;

public sealed class CsvExportService
{
    public byte[] Render(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var builder = new StringBuilder();
        if (rows.Count > 0)
        {
            var headers = rows[0].Keys.ToArray();
            builder.AppendLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
            {
                builder.AppendLine(string.Join(",", headers.Select(header => Escape(row.TryGetValue(header, out var value) ? value : null))));
            }
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(builder.ToString());
        return [.. preamble, .. content];
    }

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? "";
        if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\r') && !text.Contains('\n'))
        {
            return text;
        }

        return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
