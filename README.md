# Women's Health Data Collection App

A lightweight, local-first web application designed for the **Penrith Women's Health Centre** to streamline client data collection, session recording, and reporting.

## Key Features

- **Local-First Architecture**: Runs entirely on your local machine using a .NET 8 server and SQLite database. No internet connection required for data entry once loaded.
- **Dynamic Data Entry**: Comprehensive form for client demographics, health information, funding streams, and practitioner roles.
- **Interactive Record Viewer**: Powerful filtering system to browse through historical records.
- **Reporting & Export**: Export filtered data directly to CSV for further analysis in Excel or other tools.
- **Automatic Backups**: The system automatically creates backups of the database on every shutdown and maintains a rolling history of the last 5 backups.
- **Secure Handling**: Built-in file locking ensures only one instance of the app runs at a time, preventing database corruption.

## Technology Stack

- **Backend**: ASP.NET Core on .NET 8.
- **Frontend**: Standard HTML5, CSS3, and Vanilla JavaScript.
- **Database**: SQLite3, a reliable single-file database.
- **Automation**: Batch scripts, PowerShell, and VBScript for seamless Windows integration.

## Getting Started (Windows)

The .NET app is in `womens-health-app-dotnet`. It is optimized for Windows and provides multiple ways to start.

### Option 1: Silent Launch (Recommended)

Double-click `womens-health-app-dotnet\launch.vbs`.

- This starts the application in the background without any visible terminal windows.
- The app will automatically open in your default web browser at `http://127.0.0.1:8080`.

### Option 2: Standard Launch

Run `womens-health-app-dotnet\start.bat` or `womens-health-app-dotnet\start.ps1`.

- This will open a terminal window showing the server status and logs.
- Useful for troubleshooting if the app fails to start.

### Option 3: Terminal (Advanced Users)

Alternatively, you can run the server directly:

```powershell
dotnet run --project womens-health-app-dotnet\src\WomensHealth.App\WomensHealth.App.csproj
```

## Stopping the App

To gracefully stop the application and ensure data is backed up:

1. Click the **Exit** button in the sidebar of the web application.
2. Alternatively, run `womens-health-app-dotnet\stop.bat`.

## Project Structure

- `womens-health-app-dotnet\src\WomensHealth.App\wwwroot\WomensHealth_DataForm.html`: The main data entry interface.
- `womens-health-app-dotnet\src\WomensHealth.App\wwwroot\WomensHealth_Viewer.html`: Interface for viewing and filtering records.
- `womens-health-app-dotnet\src\WomensHealth.App\wwwroot\data.json`: Configuration for dropdown menus and hierarchical options.
- `womens-health-app-dotnet\src\WomensHealth.App\womenshealth.db`: The SQLite database file created on first run.
- `womens-health-app-dotnet\src\WomensHealth.App\backups\`: Directory where automatic database backups are stored.
- `womens-health-app-dotnet\src\WomensHealth.App\server.log`: System logs for troubleshooting.
- `server.py` and the root launch scripts: retained temporarily as the Python rollback version.

## Prerequisites

- **.NET 8 Runtime or SDK**: Required to run the converted app.
- **.NET 8 SDK**: Required to build from source and run tests.

## Rollback During Transition

The Python app is retained until the .NET version is accepted in production use.

- Run `launch.vbs` in the repo root to use the Python app.
- Run `womens-health-app-dotnet\launch.vbs` to use the .NET app.
- Copy `womenshealth.db` between app folders only when both apps are stopped.

---

*Created for Penrith Women's Health Centre.*
