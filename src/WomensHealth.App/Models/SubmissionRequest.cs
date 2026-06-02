using System.Text.Json;
using System.Text.Json.Nodes;
using WomensHealth.App.Data;

namespace WomensHealth.App.Models;

public sealed class SubmissionRequest
{
    private readonly Dictionary<string, string> _values;

    private SubmissionRequest(Dictionary<string, string> values)
    {
        _values = values;
    }

    public string SessionDate => Get("session_date");

    public string Get(string field) => _values.TryGetValue(field, out var value) ? value : "";

    public IReadOnlyList<string> GetInsertValues() => SubmissionSchema.InsertFields.Select(Get).ToArray();

    public static async Task<SubmissionRequest> ReadAsync(Stream body, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(body);
        var text = await reader.ReadToEndAsync();
        var root = JsonNode.Parse(text);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (root is JsonObject obj)
        {
            foreach (var item in obj)
            {
                values[item.Key] = ConvertValue(item.Value);
            }
        }

        return new SubmissionRequest(values);
    }

    private static string ConvertValue(JsonNode? node)
    {
        return node switch
        {
            null => "",
            JsonArray array => string.Join("|", array.Select(ConvertValue)),
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            JsonValue value => value.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            _ => node.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
        };
    }
}
