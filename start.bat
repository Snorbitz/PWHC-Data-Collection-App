@echo off
SETLOCAL EnableDelayedExpansion
TITLE Women's Health App

REM ===================================================================
REM  Women's Health App Launcher
REM  - Finds Python, frees port 8080, starts server, opens browser
REM  - On window close: CMD owns python, so Windows Job Object
REM    cascade-kills python when this window closes
REM ===================================================================

echo.
echo   Women's Health App
echo   ==================
echo.

REM -- Step 1: Find Python ------------------------------------------------
echo   Locating Python...
set PY=
python --version >nul 2>&1
if %ERRORLEVEL% EQU 0 set PY=python

if "!PY!"=="" (
    python3 --version >nul 2>&1
    if %ERRORLEVEL% EQU 0 set PY=python3
)

if "!PY!"=="" (
    echo   ERROR: Python not found in PATH.
    echo   Install Python 3 from https://python.org and try again.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('!PY! --version 2^>^&1') do echo   Found: %%v  ^(!PY!^)

REM -- Step 2: Free port 8080 if in use -----------------------------------
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

REM -- Step 3: Start server (synchronous - CMD IS the parent process)------
REM  We launch a helper PowerShell in the background ONLY for the
REM  readiness-check + browser-open, then run python synchronously here.
REM  This guarantees python is a child of this CMD window (Job Object).

echo   Starting server...
echo.

REM  Background: wait for server ready, then open browser
start /b powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$u='http://127.0.0.1:8080'; $d=(Get-Date).AddSeconds(15); while((Get-Date)-lt $d){try{$r=Invoke-WebRequest $u -UseBasicParsing -TimeoutSec 1 -EA Stop;if($r.StatusCode-lt 500){Start-Process $u;break}}catch{};Start-Sleep -ms 300}"

REM  Foreground: run python (blocks until server stops)
echo   App will open in your browser once ready.
echo   Close this window to stop the server.
echo.
!PY! server.py

REM -- Reached here means server stopped -----------------------------------
echo.
echo   Server stopped.
pause
