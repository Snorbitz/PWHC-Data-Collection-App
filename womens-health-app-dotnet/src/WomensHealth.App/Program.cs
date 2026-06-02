using WomensHealth.App;
using WomensHealth.App.Data;
using WomensHealth.App.Models;
using WomensHealth.App.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AppPaths>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<SubmissionRepository>();
builder.Services.AddSingleton<CsvExportService>();
builder.Services.AddSingleton<BackupService>();
builder.Services.AddSingleton<AppLockService>();
builder.Services.AddSingleton<ShutdownService>();

var app = builder.Build();

var appLock = app.Services.GetRequiredService<AppLockService>();
if (!app.Environment.IsEnvironment("Testing") && !appLock.TryAcquire())
{
    var currentUser = appLock.ReadCurrentOwner();
    Console.Error.WriteLine($"""

    ======================================================
    ERROR: The application is already in use by:
    [{currentUser}]

    Please ask them to close the application
    before you can start it.
    ======================================================
    """);
    return;
}

app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Services.GetRequiredService<BackupService>().BackupNow();
    app.Services.GetRequiredService<AppLockService>().Dispose();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/", (AppPaths paths) =>
{
    return File.Exists(paths.DataFormPath)
        ? Results.File(paths.DataFormPath, "text/html; charset=utf-8")
        : Results.NotFound("Error: WomensHealth_DataForm.html not found.");
});

app.MapGet("/viewer", (AppPaths paths) =>
{
    return File.Exists(paths.ViewerPath)
        ? Results.File(paths.ViewerPath, "text/html; charset=utf-8")
        : Results.NotFound("Error: WomensHealth_Viewer.html not found.");
});

app.MapGet("/api/options", (AppPaths paths) =>
{
    return File.Exists(paths.OptionsPath)
        ? Results.File(paths.OptionsPath, "application/json; charset=utf-8")
        : Results.NotFound(new { error = "data.json not found" });
});

app.MapPost("/api/submit", async (HttpContext context, SubmissionRepository repository) =>
{
    try
    {
        var request = await SubmissionRequest.ReadAsync(context.Request.Body, context.RequestAborted);
        if (string.IsNullOrWhiteSpace(request.SessionDate))
        {
            return Results.BadRequest(new { status = "error", message = "session_date is required" });
        }

        var id = repository.Insert(request);
        return Results.Ok(new { status = "ok", id });
    }
    catch
    {
        return Results.Json(new { status = "error", message = "Failed to save record" }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/records", (HttpContext context, SubmissionRepository repository) =>
{
    try
    {
        var response = repository.QueryRecords(context.Request.Query);
        return Results.Ok(new
        {
            total = response.Total,
            page = response.Page,
            per_page = response.PerPage,
            records = response.Records
        });
    }
    catch
    {
        return Results.Json(new { status = "error", message = "Internal Database Error" }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/export", (HttpContext context, SubmissionRepository repository, CsvExportService csv) =>
{
    try
    {
        var rows = repository.QueryForExport(context.Request.Query);
        var bytes = csv.Render(rows);
        var filename = $"womenshealth_export_{DateTime.Now:yyyy-MM-dd}.csv";
        return Results.File(bytes, "text/csv; charset=utf-8", filename);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapDelete("/api/record/{id}", (string id, SubmissionRepository repository) =>
{
    if (!long.TryParse(id, out var recordId))
    {
        return Results.BadRequest(new { status = "error", message = "Invalid record ID" });
    }

    return repository.Delete(recordId) switch
    {
        DeleteResult.Deleted => Results.Ok(new { status = "ok", deleted_id = recordId }),
        _ => Results.NotFound(new { status = "error", message = "Record not found" })
    };
});

app.MapPost("/api/restore", async (HttpContext context, AppPaths paths, DatabaseInitializer initializer) =>
{
    using var memory = new MemoryStream();
    await context.Request.Body.CopyToAsync(memory, context.RequestAborted);
    var uploaded = memory.ToArray();

    if (uploaded.Length == 0)
    {
        return Results.BadRequest(new { status = "error", message = "No file uploaded" });
    }

    var sqliteHeader = "SQLite format 3\0"u8.ToArray();
    if (!uploaded.AsSpan().StartsWith(sqliteHeader))
    {
        return Results.BadRequest(new { status = "error", message = "Invalid database file format" });
    }

    await File.WriteAllBytesAsync(paths.DatabasePath, uploaded, context.RequestAborted);
    initializer.Initialize();
    return Results.Ok(new { status = "ok", message = "Database restored successfully" });
});

app.MapGet("/api/shutdown", (ShutdownService shutdown) =>
{
    shutdown.StopAfterResponse();
    return Results.Ok(new { status = "ok", message = "Server shutting down..." });
});

app.Run("http://127.0.0.1:8080");

public partial class Program { }
