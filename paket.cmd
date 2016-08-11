@echo off

%~dp0\.paket\paket.bootstrapper.exe -s --max-file-age=1440
if errorlevel 1 (
  exit /b %errorlevel%
)

%~dp0\.paket\paket.exe %*