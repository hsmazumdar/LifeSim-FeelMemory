@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title LifeSim — full honest eval
echo Building (if needed) and running full honest eval...
echo.

set "SLN=%CD%\LifeSim.sln"
set "EXE=%CD%\EvolutionApp\bin\Debug\LifeSim.exe"
set "OUT=%CD%\EvalLogs\review_rerun"

set "MSBUILD="
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD (
  echo [ERROR] MSBuild not found.
  pause
  exit /b 1
)

if not exist "%EXE%" (
  echo Building Debug...
  "%MSBUILD%" "%SLN%" /p:Configuration=Debug /v:m /nologo
  if errorlevel 1 ( pause & exit /b 1 )
)

echo Out: %OUT%
"%EXE%" --eval --out "%OUT%"
echo.
echo Done. Open: %OUT%\SUMMARY.md
pause