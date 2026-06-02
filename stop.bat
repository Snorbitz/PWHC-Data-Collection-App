@echo off
SETLOCAL
TITLE Stop Women's Health App (.NET)

powershell.exe -NoProfile -Command "try { Invoke-WebRequest http://127.0.0.1:8080/api/shutdown -UseBasicParsing -TimeoutSec 2 -ErrorAction Ignore } catch {}" >nul 2>&1

timeout /t 2 /nobreak >nul

set STOPPED=0
for /f "tokens=5" %%p in ('netstat -ano 2^>nul ^| findstr ":8080 " ^| findstr "LISTENING"') do (
    taskkill /PID %%p /F >nul 2>&1
    set STOPPED=1
)

if "%STOPPED%"=="1" (
    echo Women's Health App server force stopped.
) else (
    echo Server was stopped gracefully or was not running.
)

timeout /t 2 /nobreak >nul
