@echo off
SETLOCAL EnableDelayedExpansion
TITLE Women's Health App (.NET)

echo.
echo   Women's Health App (.NET)
echo   =========================
echo.

echo   Locating .NET...
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo   ERROR: .NET was not found in PATH.
    echo   Install the .NET 8 Runtime or SDK and try again.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version 2^>^&1') do echo   Found: %%v ^(dotnet^)

echo   Checking port 8080...
set FREED=0
for /f "tokens=5" %%p in ('netstat -ano 2^>nul ^| findstr ":8080 " ^| findstr "LISTENING"') do (
    echo   Port 8080 in use by PID %%p - freeing...
    taskkill /PID %%p /F >nul 2>&1
    set FREED=1
)
if "!FREED!"=="1" (
    timeout /t 1 /nobreak >nul
    echo   Port freed.
) else (
    echo   Port 8080 is free.
)

echo   Starting app...
echo.

start /b powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$u='http://127.0.0.1:8080'; $d=(Get-Date).AddSeconds(20); while((Get-Date)-lt $d){try{$r=Invoke-WebRequest $u -UseBasicParsing -TimeoutSec 1 -EA Stop;if($r.StatusCode-lt 500){Start-Process $u;break}}catch{};Start-Sleep -ms 300}"

echo   App will open in your browser once ready.
echo   Close this window to stop the app.
echo.
dotnet run --project "%~dp0src\WomensHealth.App\WomensHealth.App.csproj"

echo.
echo   App stopped.
pause
