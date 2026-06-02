using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using WomensHealth.App.Data;
using WomensHealth.App.Models;

namespace WomensHealth.App.Tests;

public sealed class SubmissionRepositoryTests
{
    [Fact]
    public async Task InsertStoresStringAndArrayValues()
    {
        var directory = TestPaths.CreateTempDirectory();
        var paths = TestPaths.CreateAppPaths(directory);
        var initializer = new DatabaseInitializer(paths);
        initializer.Initialize();
        var repository = new SubmissionRepository(initializer);
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(
            "{\"session_date\":\"2026-06-02\",\"client_id\":\"ABC\",\"service_provided\":[\"Casework\",\"Advocacy\"]}"
        ));

        var request = await SubmissionRequest.ReadAsync(body, CancellationToken.None);
        var id = repository.Insert(request);

        Assert.True(id > 0);
        var row = Assert.Single(TestPaths.ReadAllRows(paths.DatabasePath));
        Assert.Equal("ABC", row["client_id"]);
        Assert.Equal("Casework|Advocacy", row["service_provided"]);
    }

    [Fact]
    public async Task RecordsFiltersSearchOrderAndPaginateLikePythonServer()
    {
        var directory = TestPaths.CreateTempDirectory();
        var paths = TestPaths.CreateAppPaths(directory);
        var initializer = new DatabaseInitializer(paths);
        initializer.Initialize();
        var repository = new SubmissionRepository(initializer);

        await InsertJson(repository, "{\"session_date\":\"2026-01-01\",\"client_id\":\"Alpha\",\"age\":\"30\",\"contact_mode\":\"Phone\",\"staff_member\":\"Sam\",\"service_provided\":\"Counselling\",\"practitioner\":\"Nurse\",\"evaluation_tools\":\"Tool A\"}");
        await InsertJson(repository, "{\"session_date\":\"2026-02-01\",\"client_id\":\"Beta\",\"age\":\"40\",\"contact_mode\":\"Face to Face\",\"staff_member\":\"Pat\",\"service_provided\":\"Advocacy\",\"practitioner\":\"Doctor\",\"evaluation_tools\":\"Tool B\"}");
        await InsertJson(repository, "{\"session_date\":\"2026-02-01\",\"client_id\":\"Gamma\",\"age\":\"40\",\"contact_mode\":\"Face to Face\",\"staff_member\":\"Pat\",\"service_provided\":\"Advocacy\",\"practitioner\":\"Doctor\",\"evaluation_tools\":\"Tool C\"}");

        var filtered = repository.QueryRecords(Query(("date_from", "2026-02-01"), ("age", "40"), ("staff_member", "Pa"), ("page", "1"), ("per_page", "1")));

        Assert.Equal(2, filtered.Total);
        Assert.Equal(1, filtered.Page);
        Assert.Equal(1, filtered.PerPage);
        Assert.Single(filtered.Records);
        Assert.Equal("Gamma", filtered.Records[0]["client_id"]);

        var search = repository.QueryRecords(Query(("search", "Tool A")));
        Assert.Equal(1, search.Total);
        Assert.Equal("Alpha", search.Records[0]["client_id"]);
    }

    [Fact]
    public async Task DeleteReportsMissingAndDeletesExistingRows()
    {
        var directory = TestPaths.CreateTempDirectory();
        var paths = TestPaths.CreateAppPaths(directory);
        var initializer = new DatabaseInitializer(paths);
        initializer.Initialize();
        var repository = new SubmissionRepository(initializer);
        var id = await InsertJson(repository, "{\"session_date\":\"2026-03-01\",\"client_id\":\"Delete\"}");

        Assert.Equal(DeleteResult.NotFound, repository.Delete(999999));
        Assert.Equal(DeleteResult.Deleted, repository.Delete(id));
        Assert.Empty(TestPaths.ReadAllRows(paths.DatabasePath));
    }

    private static async Task<long> InsertJson(SubmissionRepository repository, string json)
    {
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var request = await SubmissionRequest.ReadAsync(body, CancellationToken.None);
        return repository.Insert(request);
    }

    private static IQueryCollection Query(params (string Key, string Value)[] values)
    {
        return new QueryCollection(values.ToDictionary(item => item.Key, item => new StringValues(item.Value)));
    }
}
