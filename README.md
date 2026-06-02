# Women's Health Data Collection App

A lightweight, local-first web application designed for the **Penrith Women's Health Centre** to streamline client data collection, session recording, and reporting.

## Key Features

- **Local-First Architecture**: Runs entirely on your local machine using an ASP.NET Core server and SQLite database. No internet connection required for data entry once loaded.
- **Dynamic Data Entry**: Comprehensive form for client demographics, health information, funding streams, and practitioner roles.
- **Interactive Record Viewer**: Powerful filtering system to browse through historical records.
- **Reporting & Export**: Export filtered data directly to CSV for further analysis in Excel or other tools.
- **Automatic Backups**: The system automatically creates backups of the database on every shutdown and maintains a rolling history of the last 5 backups.
- **Secure Handling**: Built-in file locking ensures only one instance of the app runs at a time, preventing database corruption.

## Technology Stack

- **Backend**: ASP.NET Core on .NET 6.0 (compatible with .NET 6.0, .NET 8.0, and newer).
- **Frontend**: Standard HTML5, CSS3, and Vanilla JavaScript.
- **Database**: SQLite3, a reliable single-file database.
- **Automation**: Batch scripts, PowerShell, and VBScript for seamless Windows integration.

## Getting Started (Windows)

The application is optimized for Windows and provides multiple ways to start directly from the repository root.

### Option 1: Silent Launch (Recommended)

Double-click `launch.vbs` in the repository root.

- This starts the application in the background without any visible terminal windows.
- The app will automatically open in your default web browser at `http://127.0.0.1:8080`.

### Option 2: Standard Launch

Run `start.bat` or `start.ps1` in the repository root.

- This will open a terminal window showing the server status and logs.
- Useful for troubleshooting if the app fails to start.

### Option 3: Terminal (Advanced Users)

Alternatively, you can run the server directly:

```powershell
dotnet run --project src\WomensHealth.App\WomensHealth.App.csproj
```

## Stopping the App

To gracefully stop the application and ensure data is backed up:

1. Click the **Exit** button in the sidebar of the web application.
2. Alternatively, run `stop.bat` in the repository root.

## Project Structure

- `src\WomensHealth.App\wwwroot\WomensHealth_DataForm.html`: The main data entry interface.
- `src\WomensHealth.App\wwwroot\WomensHealth_Viewer.html`: Interface for viewing and filtering records.
- `src\WomensHealth.App\wwwroot\data.json`: Configuration for dropdown menus and options.
- `src\WomensHealth.App\womenshealth.db`: The active SQLite database file.
- `src\WomensHealth.App\backups\`: Directory where automatic database backups are stored.
- `src\WomensHealth.App\server.log`: System logs for troubleshooting.

## Prerequisites

- **.NET 6.0 SDK or newer** (such as .NET 8.0 SDK): Required to build from source and run tests.
- **.NET 6.0 Runtime or newer**: Required to run the application.

---

*Created for Penrith Women's Health Centre.*
