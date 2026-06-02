# Women's Health App .NET 8 Conversion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the existing local-first Python and static HTML Women's Health data collection app to a .NET 8 application while preserving current data, launch behavior, API contracts, and user workflows.

**Architecture:** Build an ASP.NET Core .NET 8 app that serves the existing HTML screens and exposes equivalent JSON/CSV APIs over `http://127.0.0.1:8080`. Keep SQLite as the local database, use explicit repository/service classes for database access, and migrate the current Python runtime behavior in small parity-tested slices before optional UI refactoring.

**Tech Stack:** .NET 8, ASP.NET Core Minimal APIs or MVC-style endpoints, Microsoft.Data.Sqlite, xUnit, Microsoft.AspNetCore.Mvc.Testing, static HTML/CSS/JavaScript, SQLite.

---

## Current Application Map

The current app lives in `womens-health-app` and consists of:

- `womens-health-app/server.py`: Python standard-library HTTP server, SQLite database setup, API handlers, CSV export, backup/restore, shutdown, single-instance lock, and port binding.
- `womens-health-app/WomensHealth_DataForm.html`: Data entry screen. Posts to `/api/submit`, loads option lists from `/api/options`, and calls `/api/shutdown`.
- `womens-health-app/WomensHealth_Viewer.html`: Viewer/export screen. Calls `/api/records`, `/api/export`, `/api/restore`, `/api/record/{id}`, and `/api/shutdown`.
- `womens-health-app/data.json`: Large option-list source for countries, languages, ethnicity, visa type, chronic illness, presenting issues, service provided, service type, practitioner, and evaluation tools.
- `womens-health-app/womenshealth.db`: Existing SQLite database that must remain compatible.
- `womens-health-app/start.bat`, `start.ps1`, `launch.vbs`, `stop.bat`: Windows launch and stop workflow.
- `womens-health-app/backups`: Existing SQLite backup directory.

## Migration Strategy

1. Preserve the current frontend initially. The first .NET version should serve `WomensHealth_DataForm.html`, `WomensHealth_Viewer.html`, and `data.json` with the same routes and API response shapes.
2. Preserve the existing SQLite schema and database file. No data export/import should be required for users already running the Python app.
3. Replace `server.py` behavior with .NET 8 in focused slices: startup, database schema, submit, records, export, delete, restore, backup, shutdown, launcher scripts.
4. Add tests around the behavior being converted before changing launch defaults.
5. Only after parity is verified, decide whether to modernize the frontend into Razor Pages, Blazor, React, or another UI. That is intentionally out of scope for the first conversion pass.

## Target File Structure

Create:

- `womens-health-app-dotnet/WomensHealth.App.sln`: .NET solution.
- `womens-health-app-dotnet/src/WomensHealth.App/WomensHealth.App.csproj`: ASP.NET Core .NET 8 app project.
- `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`: App startup, route registration, dependency injection, static-file setup, shutdown endpoint wiring.
- `womens-health-app-dotnet/src/WomensHealth.App/AppPaths.cs`: Resolves database, log, static asset, backup, lock, and runtime paths.
- `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionSchema.cs`: Canonical submission field names, table DDL, migration column list, filterable field list.
- `womens-health-app-dotnet/src/WomensHealth.App/Data/DatabaseInitializer.cs`: Creates and migrates `submissions`.
- `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionRepository.cs`: SQLite insert, query, delete, count, export, and restore helpers.
- `womens-health-app-dotnet/src/WomensHealth.App/Models/SubmissionRequest.cs`: Request model that accepts string or string-array JSON values.
- `womens-health-app-dotnet/src/WomensHealth.App/Models/RecordsResponse.cs`: Response model for `/api/records`.
- `womens-health-app-dotnet/src/WomensHealth.App/Services/CsvExportService.cs`: CSV rendering with UTF-8 BOM and Excel-compatible formatting.
- `womens-health-app-dotnet/src/WomensHealth.App/Services/BackupService.cs`: Backup current DB and keep the latest five backups.
- `womens-health-app-dotnet/src/WomensHealth.App/Services/AppLockService.cs`: Single-instance lock using a named mutex on Windows.
- `womens-health-app-dotnet/src/WomensHealth.App/Services/ShutdownService.cs`: Coordinates graceful shutdown after HTTP response.
- `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_DataForm.html`: Copy of current data entry HTML.
- `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_Viewer.html`: Copy of current viewer HTML.
- `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/data.json`: Copy of current option-list data.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/WomensHealth.App.Tests.csproj`: xUnit test project.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/DatabaseInitializerTests.cs`: Schema and migration tests.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/SubmissionRepositoryTests.cs`: Insert/query/delete/filter tests.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`: End-to-end HTTP contract tests.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/CsvExportTests.cs`: CSV export tests.
- `womens-health-app-dotnet/tests/WomensHealth.App.Tests/BackupServiceTests.cs`: Backup retention tests.
- `womens-health-app-dotnet/start.bat`: .NET launcher replacement.
- `womens-health-app-dotnet/start.ps1`: PowerShell launcher replacement.
- `womens-health-app-dotnet/launch.vbs`: Silent launcher replacement.
- `womens-health-app-dotnet/stop.bat`: Stop script replacement.

Modify after parity:

- `README.md`: Replace Python setup instructions with .NET 8 setup and launch instructions.
- Optional: keep `womens-health-app/server.py` during the transition, then archive it only after the .NET app is verified.

## API Contract To Preserve

The .NET app must keep these routes and behavior:

- `GET /`: returns `WomensHealth_DataForm.html` with `text/html; charset=utf-8`.
- `GET /viewer`: returns `WomensHealth_Viewer.html` with `text/html; charset=utf-8`.
- `GET /api/options`: returns `data.json` with `application/json; charset=utf-8`.
- `POST /api/submit`: accepts the existing JSON payload, joins array values with `|`, requires `session_date`, inserts a row, returns `{ "status": "ok", "id": number }`.
- `GET /api/records`: accepts current query filters, returns `{ "total": number, "page": number, "per_page": number, "records": [...] }`.
- `GET /api/export`: accepts the same filters as `/api/records`, returns UTF-8 BOM CSV named `womenshealth_export_YYYY-MM-DD.csv`.
- `DELETE /api/record/{id}`: validates integer ID, returns 400 for invalid ID, 404 when missing, and `{ "status": "ok", "deleted_id": id }` after delete.
- `POST /api/restore`: accepts raw SQLite database bytes, validates the SQLite header, overwrites the database, and returns `{ "status": "ok", "message": "Database restored successfully" }`.
- `GET /api/shutdown`: returns `{ "status": "ok", "message": "Server shutting down..." }`, then shuts down the app after the response is sent.

## Database Contract To Preserve

The `submissions` table must remain compatible with the current `womenshealth.db`.

Required columns:

```text
id INTEGER PRIMARY KEY AUTOINCREMENT
submitted_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
session_date TEXT NOT NULL
client_id TEXT
age TEXT
contact_mode TEXT
country TEXT
language TEXT
income_source TEXT
visa_type TEXT
ethnicity TEXT
disability TEXT
chronic_illness TEXT
presenting_issues TEXT
service_provided TEXT
service_type TEXT
practitioner TEXT
group_type TEXT
evaluation_tools TEXT
staff_member TEXT
client_status TEXT
visit_number TEXT
carer TEXT DEFAULT 'No'
financial_hardship TEXT DEFAULT 'No'
social_isolation TEXT DEFAULT 'No'
rural_postcode TEXT DEFAULT 'No'
lgbtiq TEXT DEFAULT 'No'
funding_stream TEXT
funding_option TEXT
```

Use `PRAGMA journal_mode=WAL` when opening connections, matching the current Python app.

## Task 1: Create .NET Solution Skeleton

**Files:**
- Create: `womens-health-app-dotnet/WomensHealth.App.sln`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/WomensHealth.App.csproj`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Create: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/WomensHealth.App.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n WomensHealth.App -o womens-health-app-dotnet
dotnet new web -n WomensHealth.App -o womens-health-app-dotnet/src/WomensHealth.App --framework net8.0
dotnet new xunit -n WomensHealth.App.Tests -o womens-health-app-dotnet/tests/WomensHealth.App.Tests --framework net8.0
dotnet sln womens-health-app-dotnet/WomensHealth.App.sln add womens-health-app-dotnet/src/WomensHealth.App/WomensHealth.App.csproj
dotnet sln womens-health-app-dotnet/WomensHealth.App.sln add womens-health-app-dotnet/tests/WomensHealth.App.Tests/WomensHealth.App.Tests.csproj
dotnet add womens-health-app-dotnet/tests/WomensHealth.App.Tests/WomensHealth.App.Tests.csproj reference womens-health-app-dotnet/src/WomensHealth.App/WomensHealth.App.csproj
dotnet add womens-health-app-dotnet/src/WomensHealth.App/WomensHealth.App.csproj package Microsoft.Data.Sqlite
dotnet add womens-health-app-dotnet/tests/WomensHealth.App.Tests/WomensHealth.App.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

Expected: solution restores and both projects target `net8.0`.

- [ ] **Step 2: Replace initial `Program.cs` with a minimal health route**

Use this starting point in `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run("http://127.0.0.1:8080");

public partial class Program { }
```

- [ ] **Step 3: Verify skeleton**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
```

Expected: test project builds and the default xUnit test passes.

- [ ] **Step 4: Commit**

```powershell
git add womens-health-app-dotnet
git commit -m "chore: create dotnet 8 app skeleton"
```

## Task 2: Add Path Resolution And Static Asset Serving

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/AppPaths.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_DataForm.html`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_Viewer.html`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/wwwroot/data.json`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Copy static assets**

Copy these files unchanged:

```powershell
Copy-Item womens-health-app/WomensHealth_DataForm.html womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_DataForm.html
Copy-Item womens-health-app/WomensHealth_Viewer.html womens-health-app-dotnet/src/WomensHealth.App/wwwroot/WomensHealth_Viewer.html
Copy-Item womens-health-app/data.json womens-health-app-dotnet/src/WomensHealth.App/wwwroot/data.json
```

- [ ] **Step 2: Add `AppPaths`**

```csharp
public sealed class AppPaths
{
    public AppPaths(IWebHostEnvironment environment)
    {
        AppDirectory = environment.ContentRootPath;
        WebRootDirectory = environment.WebRootPath ?? Path.Combine(AppDirectory, "wwwroot");
        DatabasePath = Path.Combine(AppDirectory, "womenshealth.db");
        LogPath = Path.Combine(AppDirectory, "server.log");
        BackupDirectory = Path.Combine(AppDirectory, "backups");
        LockInfoPath = Path.Combine(AppDirectory, "server.info");
        DataFormPath = Path.Combine(WebRootDirectory, "WomensHealth_DataForm.html");
        ViewerPath = Path.Combine(WebRootDirectory, "WomensHealth_Viewer.html");
        OptionsPath = Path.Combine(WebRootDirectory, "data.json");
    }

    public string AppDirectory { get; }
    public string WebRootDirectory { get; }
    public string DatabasePath { get; }
    public string LogPath { get; }
    public string BackupDirectory { get; }
    public string LockInfoPath { get; }
    public string DataFormPath { get; }
    public string ViewerPath { get; }
    public string OptionsPath { get; }
}
```

- [ ] **Step 3: Add tests for static routes**

Create `ApiContractTests.cs` with tests that assert:

```text
GET / returns 200 and contains "Client Data Entry Form"
GET /viewer returns 200 and contains "Record Viewer"
GET /api/options returns 200 and contains keys "country", "language", and "practitioner"
```

- [ ] **Step 4: Implement static routes**

Register `AppPaths` and map:

```csharp
builder.Services.AddSingleton<AppPaths>();

app.MapGet("/", async (AppPaths paths) =>
{
    return File.Exists(paths.DataFormPath)
        ? Results.File(paths.DataFormPath, "text/html; charset=utf-8")
        : Results.NotFound("Error: WomensHealth_DataForm.html not found.");
});

app.MapGet("/viewer", async (AppPaths paths) =>
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
```

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: serve static app screens from dotnet"
```

Expected: static route tests pass.

## Task 3: Port SQLite Schema Initialization

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionSchema.cs`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Data/DatabaseInitializer.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/DatabaseInitializerTests.cs`

- [ ] **Step 1: Write schema tests**

Assert that initialization creates `submissions` with every column listed in "Database Contract To Preserve".

Also create a test database with only the original pre-migration columns, run initialization, and assert it adds:

```text
staff_member, client_status, visit_number, carer, financial_hardship, social_isolation, rural_postcode, lgbtiq, funding_stream, funding_option
```

- [ ] **Step 2: Implement schema constants**

`SubmissionSchema` should expose:

```csharp
public static readonly string[] InsertFields =
[
    "session_date", "client_id", "staff_member", "client_status", "visit_number",
    "age", "carer", "financial_hardship", "social_isolation", "rural_postcode", "lgbtiq",
    "funding_stream", "funding_option", "contact_mode", "country", "language",
    "income_source", "visa_type", "ethnicity", "disability", "chronic_illness",
    "presenting_issues", "service_provided", "service_type", "practitioner",
    "group_type", "evaluation_tools"
];
```

Include separate arrays for exact filters, LIKE filters, and search fields matching `server.py`.

- [ ] **Step 3: Implement database initializer**

Use `Microsoft.Data.Sqlite`. Open a connection to `Data Source={DatabasePath}`, execute `PRAGMA journal_mode=WAL`, create the table, inspect `PRAGMA table_info(submissions)`, and run the same `ALTER TABLE` migrations as `server.py`.

- [ ] **Step 4: Register and run initialization at startup**

In `Program.cs`, register `DatabaseInitializer` and call it before `app.Run(...)`.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: initialize sqlite schema in dotnet"
```

Expected: schema tests pass and an empty `womenshealth.db` is created when running the app.

## Task 4: Port Submit Endpoint

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Models/SubmissionRequest.cs`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionRepository.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/SubmissionRepositoryTests.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for:

```text
POST /api/submit without session_date returns 400 with status error
POST /api/submit with string values inserts a row and returns status ok plus id
POST /api/submit with array values stores them joined with |
```

- [ ] **Step 2: Implement flexible request parsing**

Use `JsonObject` or a custom converter so values can be read as:

```text
missing -> ""
string -> string value
array of strings -> item1|item2
other JSON values -> string representation
```

- [ ] **Step 3: Implement repository insert**

Build parameterized SQL using `SubmissionSchema.InsertFields`. Never interpolate user values into SQL. Return the inserted row ID using `last_insert_rowid()`.

- [ ] **Step 4: Implement `/api/submit`**

Map `POST /api/submit`, validate `session_date`, call repository insert, return the same JSON response as Python.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: port submit endpoint to dotnet"
```

Expected: submit tests pass.

## Task 5: Port Records Query And Filtering

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Models/RecordsResponse.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionRepository.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/SubmissionRepositoryTests.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing filter tests**

Seed records and assert:

```text
date_from filters session_date >= value
date_to filters session_date <= value
age uses exact match
contact_mode uses exact match
client_id, staff_member, service_provided, practitioner, and evaluation_tools use LIKE
search scans all text fields listed in server.py
results order by session_date DESC, id DESC
pagination returns total, page, per_page, and only requested rows
```

- [ ] **Step 2: Implement safe WHERE builder**

Create a repository helper that accepts `IQueryCollection`, validates requested field names against `SubmissionSchema`, and returns SQL plus parameters.

- [ ] **Step 3: Implement `/api/records`**

Default `page` to `1`, `per_page` to `50`, calculate offset, query total and records, then return:

```json
{
  "total": 0,
  "page": 1,
  "per_page": 50,
  "records": []
}
```

- [ ] **Step 4: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: port record filtering endpoint"
```

Expected: record query and API contract tests pass.

## Task 6: Port Delete Endpoint

**Files:**
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionRepository.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for:

```text
DELETE /api/record/not-a-number returns 400
DELETE /api/record/999999 returns 404 when missing
DELETE /api/record/{existingId} removes the row and returns deleted_id
```

- [ ] **Step 2: Implement repository delete**

Check existence first with `SELECT id FROM submissions WHERE id = @id`, then delete using a parameterized statement.

- [ ] **Step 3: Implement route**

Map `DELETE /api/record/{id}` and preserve the existing response shapes.

- [ ] **Step 4: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: port record delete endpoint"
```

Expected: delete tests pass.

## Task 7: Port CSV Export

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Services/CsvExportService.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Data/SubmissionRepository.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/CsvExportTests.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing CSV tests**

Assert:

```text
response starts with UTF-8 BOM
Content-Type is text/csv; charset=utf-8
Content-Disposition filename starts with womenshealth_export_
header row matches SQLite row column order
quoted fields escape commas, quotes, and newlines correctly
filters are applied exactly like /api/records
```

- [ ] **Step 2: Implement CSV service**

Use a small CSV writer that quotes fields containing comma, quote, CR, or LF and doubles embedded quotes.

- [ ] **Step 3: Implement `/api/export`**

Reuse the same WHERE builder as `/api/records`, omit pagination, order by `session_date DESC, id DESC`, and return file bytes.

- [ ] **Step 4: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: port csv export"
```

Expected: CSV tests pass.

## Task 8: Port Restore, Backup, Shutdown, And Single-Instance Behavior

**Files:**
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Services/BackupService.cs`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Services/AppLockService.cs`
- Create: `womens-health-app-dotnet/src/WomensHealth.App/Services/ShutdownService.cs`
- Modify: `womens-health-app-dotnet/src/WomensHealth.App/Program.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/BackupServiceTests.cs`
- Test: `womens-health-app-dotnet/tests/WomensHealth.App.Tests/ApiContractTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for:

```text
POST /api/restore with empty body returns 400
POST /api/restore with non-SQLite bytes returns 400
POST /api/restore with bytes starting "SQLite format 3\0" replaces the database file
backup creates backups/womenshealth_backup_YYYYMMDD_HHMMSS.db
backup retention keeps only five matching backup files
```

- [ ] **Step 2: Implement restore**

Read the raw body, reject empty body, validate the SQLite magic header, close active connections before replacing the DB file, write bytes to `AppPaths.DatabasePath`.

- [ ] **Step 3: Implement backup service**

On shutdown, if the DB exists, copy it to `backups`, then delete older backups beyond the newest five by filename sort.

- [ ] **Step 4: Implement shutdown route**

Use `IHostApplicationLifetime.StopApplication()` after sending the JSON response. Preserve the frontend behavior where the page changes to "Server has been shut down."

- [ ] **Step 5: Implement single-instance lock**

On Windows, use a named mutex such as `Global\WomensHealthAppLocal8080` when available. Write `server.info` with `{username} on {machine}`. If another instance owns the mutex, print the same user-facing error style as `server.py` and exit.

- [ ] **Step 6: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add womens-health-app-dotnet
git commit -m "feat: port lifecycle and backup behavior"
```

Expected: restore and backup tests pass.

## Task 9: Replace Windows Launch Scripts

**Files:**
- Create: `womens-health-app-dotnet/start.bat`
- Create: `womens-health-app-dotnet/start.ps1`
- Create: `womens-health-app-dotnet/launch.vbs`
- Create: `womens-health-app-dotnet/stop.bat`
- Modify: `README.md`

- [ ] **Step 1: Create PowerShell launcher**

Port the current `start.ps1` behavior with these .NET-specific changes:

```text
locate dotnet instead of python
free port 8080 if in use
run dotnet run --project src/WomensHealth.App/WomensHealth.App.csproj
wait for http://127.0.0.1:8080
open browser after readiness
stop child process on Ctrl+C or window close
```

- [ ] **Step 2: Create batch launcher**

Port `start.bat` to locate `dotnet`, free port 8080, start the .NET app, and open the browser after readiness.

- [ ] **Step 3: Create silent launcher**

Keep `launch.vbs` as a hidden PowerShell launcher that calls the new `start.ps1`.

- [ ] **Step 4: Create stop script**

Keep the graceful `/api/shutdown` call first, wait two seconds, then force-kill any remaining process listening on port 8080.

- [ ] **Step 5: Update README**

Document:

```text
.NET 8 Runtime or SDK required
double-click launch.vbs to start
double-click stop.bat to stop
data remains local in womenshealth.db
existing Python version is retained only for rollback until the .NET version is accepted
```

- [ ] **Step 6: Verify and commit**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
git add README.md womens-health-app-dotnet
git commit -m "chore: add dotnet launch workflow"
```

Expected: launch scripts start the app at `http://127.0.0.1:8080` and stop it cleanly.

## Task 10: Manual Parity Verification

**Files:**
- Modify only if verification finds a defect.

- [ ] **Step 1: Start the .NET app**

Run:

```powershell
womens-health-app-dotnet/start.ps1
```

Expected: browser opens to `http://127.0.0.1:8080`.

- [ ] **Step 2: Verify data entry**

In the form:

```text
enter a session date
enter a client ID
choose at least one tiered practitioner option
choose at least one service provided option
submit
confirm success toast appears
confirm the form clears
```

- [ ] **Step 3: Verify viewer**

Open `/viewer` and confirm:

```text
new record appears
filters reduce results
free-text search finds the record
pagination controls remain stable
delete opens the confirmation modal
confirmed delete removes the row
```

- [ ] **Step 4: Verify CSV export**

Export filtered records and confirm:

```text
file downloads
filename starts womenshealth_export_
Excel opens the file without mojibake
columns match the viewer table data
```

- [ ] **Step 5: Verify restore and backup**

Use a copied SQLite database through Restore DB and confirm:

```text
restore succeeds
viewer reloads against restored data
stopping the app creates a backup in backups
only the newest five backups remain after repeated stops
```

- [ ] **Step 6: Commit any fixes**

```powershell
git add womens-health-app-dotnet README.md
git commit -m "fix: resolve dotnet parity issues"
```

Expected: no commit is needed if all parity checks pass first time.

## Task 11: Cutover And Rollback

**Files:**
- Modify: `README.md`
- Optional modify: root-level screenshots or docs if they point to Python-specific instructions.

- [ ] **Step 1: Define accepted app location**

Choose one:

```text
Option A: keep .NET app in womens-health-app-dotnet and leave Python app untouched for rollback
Option B: after acceptance, rename folders so the .NET app becomes womens-health-app
```

Recommended: Option A for the first release, because existing data and rollback are safer.

- [ ] **Step 2: Rollback plan**

Keep the Python app usable until the .NET app has passed production use:

```text
run womens-health-app/launch.vbs to use the Python app
run womens-health-app-dotnet/launch.vbs to use the .NET app
copy womenshealth.db between app folders only when both apps are stopped
```

- [ ] **Step 3: Final verification**

Run:

```powershell
dotnet test womens-health-app-dotnet/WomensHealth.App.sln
```

Expected: all tests pass.

- [ ] **Step 4: Commit cutover documentation**

```powershell
git add README.md conversion.md
git commit -m "docs: document dotnet cutover plan"
```

## Risk Register

- **SQLite file replacement while connections are open:** Restore must close active connections or use short-lived connections per repository method.
- **Array field compatibility:** Current frontend sends arrays for multi-select fields; .NET parsing must join them with `|` exactly like Python.
- **Filter SQL safety:** Field names must come from whitelisted schema constants. User query-string keys must never be interpolated directly.
- **CSV encoding:** Keep the UTF-8 BOM for Excel compatibility.
- **Launch behavior:** Users currently double-click `launch.vbs`; the .NET app must preserve that non-technical workflow.
- **Port conflict handling:** Existing scripts force-free port 8080. Keep that behavior in launch scripts, and keep app-level "port already in use" messaging.
- **Existing database compatibility:** Do not switch to Entity Framework migrations unless there is a deliberate migration plan. Direct SQLite commands are simpler and closer to the current app.

## Acceptance Criteria

- `dotnet test womens-health-app-dotnet/WomensHealth.App.sln` passes.
- `womens-health-app-dotnet/launch.vbs` starts the app silently and opens the browser.
- `/`, `/viewer`, `/api/options`, `/api/submit`, `/api/records`, `/api/export`, `/api/record/{id}`, `/api/restore`, and `/api/shutdown` behave like the Python version.
- Existing `womenshealth.db` can be copied into the .NET app folder and read without migration errors.
- Submitting, filtering, exporting, deleting, restoring, stopping, and backup retention all pass manual parity checks.
- README explains the .NET 8 requirement and the new launch workflow.
