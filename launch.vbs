' Women's Health App - Silent Launcher
' Double-click this file to start the app with NO terminal window.
' The browser will open automatically when the server is ready.
' To stop the server, run stop.bat

Dim oShell, sDir
Set oShell = CreateObject("WScript.Shell")

' Get the directory this .vbs file lives in
sDir = Left(WScript.ScriptFullName, InStrRev(WScript.ScriptFullName, "\"))

' Run PowerShell completely hidden (window style 0 = invisible)
oShell.Run "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & _
           sDir & "start.ps1""", 0, False

Set oShell = Nothing
