# Women's Health App - .NET PowerShell Launcher
# - Finds dotnet automatically
# - Frees port 8080 if already in use
# - Waits until the app is ready before opening the browser
# - Cleans up on Ctrl+C

$Port = 8080
$Url = "http://127.0.0.1:$Port"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $ScriptDir "src\WomensHealth.App\WomensHealth.App.csproj"

function Write-Status {
    param([string]$Message, [string]$Colour = 'Cyan')
    Write-Host "  $Message" -ForegroundColor $Colour
}

Write-Host ""
Write-Host "  Women's Health App (.NET)" -ForegroundColor Magenta
Write-Host "  =========================" -ForegroundColor DarkMagenta
Write-Host ""

Write-Status "Locating .NET..."
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Status "ERROR: .NET was not found in PATH. Install the .NET 8 Runtime or SDK and try again." 'Red'
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Status "Found: $(& dotnet --version)  (dotnet)" 'Green'

Write-Status "Checking port $Port..."
$existing = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
    Where-Object { $_.State -eq 'Listen' } |
    Select-Object -ExpandProperty OwningProcess -First 1
if ($existing) {
    Write-Status "Port $Port in use by PID $existing - freeing..." 'Yellow'
    Stop-Process -Id $existing -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    Write-Status "Port freed." 'Yellow'
}
else {
    Write-Status "Port $Port is free." 'Green'
}

Write-Status "Starting app..." 'Cyan'
$startArgs = @{
    FilePath         = "dotnet"
    ArgumentList     = @("run", "--project", $ProjectPath)
    WorkingDirectory = $ScriptDir
    PassThru         = $true
    NoNewWindow      = $true
}
$script:appProcess = Start-Process @startArgs
Write-Status "App PID: $($script:appProcess.Id)" 'DarkCyan'

function Stop-App {
    if ($null -ne $script:appProcess -and -not $script:appProcess.HasExited) {
        Write-Host ""
        Write-Host "  Stopping app (PID $($script:appProcess.Id))..." -ForegroundColor Yellow
        try {
            Invoke-WebRequest -Uri "$Url/api/shutdown" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue | Out-Null
        } catch {}
        Start-Sleep -Seconds 2
        if (-not $script:appProcess.HasExited) {
            try { $script:appProcess.Kill() } catch {}
            $script:appProcess.WaitForExit(3000)
        }
    }

    $leftover = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
        Where-Object { $_.State -eq 'Listen' } |
        Select-Object -ExpandProperty OwningProcess -First 1
    if ($leftover) {
        Stop-Process -Id $leftover -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  App stopped. Goodbye!" -ForegroundColor DarkGray
}

[Console]::TreatControlCAsInput = $false
trap {
    Stop-App
    break
}

Write-Status "Waiting for app to be ready..." 'Cyan'
$ready = $false
$deadline = (Get-Date).AddSeconds(20)
while ((Get-Date) -lt $deadline) {
    if ($script:appProcess.HasExited) {
        Write-Status "ERROR: App exited prematurely (code $($script:appProcess.ExitCode))." 'Red'
        Read-Host "Press Enter to exit"
        exit 1
    }
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($response.StatusCode -lt 500) {
            $ready = $true
            break
        }
    }
    catch {}
    Start-Sleep -Milliseconds 250
}

if (-not $ready) {
    Write-Status "ERROR: App did not respond within 20 seconds." 'Red'
    Stop-App
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Status "App is ready!" 'Green'
Write-Status "Opening $Url in your browser..." 'Cyan'
Start-Process $Url

Write-Host ""
Write-Host "  App running at $Url" -ForegroundColor Green
Write-Host "  Press Ctrl+C in this window to stop the app." -ForegroundColor DarkGray
Write-Host ""

try {
    $script:appProcess.WaitForExit()
    if ($script:appProcess.ExitCode -ne 0) {
        Write-Status "App stopped (exit code $($script:appProcess.ExitCode))." 'Yellow'
        Read-Host "Press Enter to close"
    }
}
finally {
    Stop-App
}
