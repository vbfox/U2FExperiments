@echo off
setlocal enableextensions enabledelayedexpansion

call paket.cmd restore
if errorlevel 1 (
  exit /b %errorlevel%
)

msbuild %~dp0\U2FExperiments.sln