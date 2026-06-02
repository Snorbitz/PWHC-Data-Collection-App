using System.Text;
using WomensHealth.App.Services;

namespace WomensHealth.App.Tests;

public sealed class CsvExportTests
{
    [Fact]
    public void RenderStartsWithBomAndEscapesCsvFields()
    {
        var service = new CsvExportService();
        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1,
                ["client_id"] = "Client, \"Quoted\"",
                ["notes"] = "Line 1\nLine 2"
            }
        };

        var bytes = service.Render(rows);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3).ToArray());
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("id,client_id,notes", text);
        Assert.Contains("\"Client, \"\"Quoted\"\"\"", text);
        Assert.Contains("\"Line 1\nLine 2\"", text);
    }
}
