using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace WomensHealth.App.Tests;

public sealed class ApiContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiContractTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StaticRoutesPreserveCurrentScreensAndOptions()
    {
        var form = await _client.GetStringAsync("/");
        Assert.Contains("Client Data Entry Form", form);

        var viewer = await _client.GetStringAsync("/viewer");
        Assert.Contains("Record Viewer", viewer);

        var options = await _client.GetStringAsync("/api/options");
        Assert.Contains("\"country\"", options);
        Assert.Contains("\"language\"", options);
        Assert.Contains("\"practitioner\"", options);
    }

    [Fact]
    public async Task SubmitRequiresSessionDate()
    {
        var response = await _client.PostAsJsonAsync("/api/submit", new { client_id = "C-001" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("session_date is required", body);
    }

    [Fact]
    public async Task SubmitAndRecordsPreserveResponseShape()
    {
        var submit = await _client.PostAsJsonAsync("/api/submit", new
        {
            session_date = "2026-06-02",
            client_id = "C-001",
            practitioner = new[] { "Nurse", "Counsellor" }
        });

        submit.EnsureSuccessStatusCode();
        var submitJson = await submit.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", submitJson);

        var records = await _client.GetFromJsonAsync<JsonElement>("/api/records?client_id=C-001");
        Assert.Equal(1, records.GetProperty("total").GetInt32());
        Assert.Equal(1, records.GetProperty("page").GetInt32());
        Assert.Equal(50, records.GetProperty("per_page").GetInt32());
        Assert.Equal("Nurse|Counsellor", records.GetProperty("records")[0].GetProperty("practitioner").GetString());
    }

    [Fact]
    public async Task DeleteEndpointValidatesMissingAndDeletesExistingRecord()
    {
        var invalid = await _client.DeleteAsync("/api/record/not-a-number");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var missing = await _client.DeleteAsync("/api/record/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var id = await TestPaths.SubmitAsync(_client, new { session_date = "2026-06-03", client_id = "Delete-Me" });
        var deleted = await _client.DeleteAsync($"/api/record/{id}");
        deleted.EnsureSuccessStatusCode();
        var body = await deleted.Content.ReadAsStringAsync();
        Assert.Contains("\"deleted_id\":" + id, body);
    }

    [Fact]
    public async Task ExportReturnsUtf8BomCsvAttachment()
    {
        await TestPaths.SubmitAsync(_client, new { session_date = "2026-06-04", client_id = "CSV, Client" });

        var response = await _client.GetAsync("/api/export?client_id=CSV");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv; charset=utf-8", response.Content.Headers.ContentType!.ToString());
        Assert.StartsWith("attachment; filename=womenshealth_export_", response.Content.Headers.ContentDisposition!.ToString());
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3).ToArray());
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"CSV, Client\"", text);
    }

    [Fact]
    public async Task RestoreValidatesRawSqliteBytes()
    {
        var empty = await _client.PostAsync("/api/restore", new ByteArrayContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var invalid = await _client.PostAsync("/api/restore", new ByteArrayContent(Encoding.UTF8.GetBytes("not sqlite")));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task ShutdownReturnsExpectedPayload()
    {
        var response = await _client.GetAsync("/api/shutdown");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Server shutting down", body);
    }
}
