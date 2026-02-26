# Women's Health App - PowerShell Launcher
# - Finds Python automatically
# - Frees port 8080 if already in use
# - Waits until server is truly ready before opening browser
# - Cleans up on Ctrl+C (and attempts cleanup on window close)

$Port = 8080
$Url = "http://127.0.0.1:$Port"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Status {
    param([string]$msg, [string]$colour = 'Cyan')
    Write-Host "  $msg" -ForegroundColor $colour
}

Write-Host ""
Write-Host "  Women's Health App" -ForegroundColor Magenta
Write-Host "  ==================" -ForegroundColor DarkMagenta
Write-Host ""

# -- Step 1: Locate Python ----------------------------------------------------
Write-Status "Locating Python..."
$python = $null
foreach ($candidate in @('python', 'python3')) {
    try {
        $ver = & $candidate --version 2>&1
        if ($LASTEXITCODE -eq 0 -and ($ver -match 'Python')) {
            $python = $candidate
            break
        }
    }
    catch {}
}
if (-not $python) {
    Write-Status "ERROR: Python not found in PATH. Install Python 3 and try again." 'Red'
    Read-Host "Press Enter to exit"
    exit 1
}
$verStr = (& $python --version 2>&1)
Write-Status "Found: $verStr  ($python)" 'Green'

# -- Step 2: Free port if already in use --------------------------------------
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

# -- Step 3: Start server (as CHILD of this process so Job Object covers it) --
Write-Status "Starting server..." 'Cyan'
$startArgs = @{
    FilePath         = $python
    ArgumentList     = 'server.py'
    WorkingDirectory = $ScriptDir
    PassThru         = $true
    NoNewWindow      = $true   # same console = same Job Object
}
$script:srv = Start-Process @startArgs
Write-Status "Server PID: $($script:srv.Id)" 'DarkCyan'

# -- Cleanup function (called explicitly on Ctrl+C and clean exits) -----------
function Stop-Server {
    if ($null -ne $script:srv -and -not $script:srv.HasExited) {
        Write-Host ""
        Write-Host "  Stopping server (PID $($script:srv.Id))..." -ForegroundColor Yellow
        try { $script:srv.Kill() } catch {}
        $script:srv.WaitForExit(3000)
    }
    # Belt-and-braces: also sweep port
    $leftover = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
    Where-Object { $_.State -eq 'Listen' } |
    Select-Object -ExpandProperty OwningProcess -First 1
    if ($leftover) {
        Stop-Process -Id $leftover -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  Server stopped. Goodbye!" -ForegroundColor DarkGray
}

# Ctrl+C trap (fires reliably for keyboard interrupt)
[Console]::TreatControlCAsInput = $false
trap {
    Stop-Server
    break
}

# -- Step 4: Wait for HTTP readiness (up to 10 s) -----------------------------
Write-Status "Waiting for server to be ready..." 'Cyan'
$ready = $false
$deadline = (Get-Date).AddSeconds(10)
while ((Get-Date) -lt $deadline) {
    if ($script:srv.HasExited) {
        Write-Status "ERROR: Server exited prematurely (code $($script:srv.ExitCode))." 'Red'
        Write-Status "Check server.py and womenshealth.log for details." 'Red'
        Read-Host "Press Enter to exit"
        exit 1
    }
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($r.StatusCode -lt 500) { $ready = $true; break }
    }
    catch {}
    Start-Sleep -Milliseconds 200
}
if (-not $ready) {
    Write-Status "ERROR: Server did not respond within 10 s." 'Red'
    Stop-Server
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Status "Server is ready!" 'Green'

# -- Step 5: Open browser -----------------------------------------------------
Write-Status "Opening $Url in your browser..." 'Cyan'
Start-Process $Url

Write-Host ""
Write-Host "  App running at $Url" -ForegroundColor Green
Write-Host "  Press Ctrl+C in this window to stop the server." -ForegroundColor DarkGray
Write-Host ""

# -- Step 6: Block here; let trap handle Ctrl+C; finally handles everything else
try {
    $script:srv.WaitForExit()
    if ($script:srv.ExitCode -ne 0) {
        Write-Status "Server stopped (exit code $($script:srv.ExitCode))." 'Yellow'
        Read-Host "Press Enter to close"
    }
}
finally {
    Stop-Server
}
