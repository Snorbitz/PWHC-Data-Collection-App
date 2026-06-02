using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using WomensHealth.App;
using WomensHealth.App.Data;

namespace WomensHealth.App.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;
    private readonly string _backupDirectory;

    public TestWebApplicationFactory()
    {
        var directory = TestPaths.CreateTempDirectory();
        _databasePath = Path.Combine(directory, "womenshealth.db");
        _backupDirectory = Path.Combine(directory, "backups");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WomensHealth:DatabasePath"] = _databasePath,
                ["WomensHealth:BackupDirectory"] = _backupDirectory
            });
        });
    }
}

public static class TestPaths
{
    public static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "WomensHealth.App.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static AppPaths CreateAppPaths(string directory)
    {
        var environment = new TestEnvironment
        {
            ContentRootPath = directory,
            WebRootPath = Path.Combine(directory, "wwwroot")
        };
        var configuration = new ConfigurationBuilder().Build();
        return new AppPaths(environment, configuration);
    }

    public static async Task<long> SubmitAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/submit", body);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return Convert.ToInt64(json!["id"].ToString());
    }

    public static List<Dictionary<string, object?>> ReadAllRows(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM submissions ORDER BY id";
        using var reader = command.ExecuteReader();

        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }

        return rows;
    }
}

public sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "WomensHealth.App";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
