' Women's Health App - .NET Silent Launcher
' Double-click this file to start the app with no terminal window.
' The browser will open automatically when the app is ready.
' To stop the app, run stop.bat.

Dim oShell, sDir
Set oShell = CreateObject("WScript.Shell")

sDir = Left(WScript.ScriptFullName, InStrRev(WScript.ScriptFullName, "\"))

oShell.Run "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & _
           sDir & "start.ps1""", 0, False

Set oShell = Nothing
