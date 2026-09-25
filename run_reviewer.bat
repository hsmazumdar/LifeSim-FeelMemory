@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

title LifeSim — reviewer launcher
echo.
echo  LifeSim reviewer launcher
echo  Repo: %CD%
echo.

set "SLN=%CD%\LifeSim.sln"
set "EXE=%CD%\EvolutionApp\bin\Debug\LifeSim.exe"
set "OUT_ROOT=%CD%\EvalLogs"

call :find_msbuild
if errorlevel 1 (
  echo [ERROR] MSBuild not found. Install Visual Studio 2022 Build Tools / .NET desktop workload.
  pause
  exit /b 1
)

:menu
echo.
echo  ----------------------------------------
echo   1  Build Debug
echo   2  Full honest eval  (--eval)     [paper numbers]
echo   3  Goal-known LOO only (--gk-loo)
echo   4  Smoke self-test     (--smoke)
echo   5  Teacher diagnostic  (--teacher-diag)
echo   6  Open GUI
echo   7  Build + full eval
echo   0  Exit
echo  ----------------------------------------
set "CHOICE="
set /p CHOICE=  Choose [0-7]: 

if "%CHOICE%"=="1" goto do_build
if "%CHOICE%"=="2" goto do_eval
if "%CHOICE%"=="3" goto do_gk
if "%CHOICE%"=="4" goto do_smoke
if "%CHOICE%"=="5" goto do_teacher
if "%CHOICE%"=="6" goto do_gui
if "%CHOICE%"=="7" goto do_build_eval
if "%CHOICE%"=="0" exit /b 0
echo  Invalid choice.
goto menu

:do_build
call :build
goto menu

:do_build_eval
call :build
if errorlevel 1 goto menu
goto do_eval

:do_eval
call :ensure_exe
if errorlevel 1 goto menu
set "OUT=%OUT_ROOT%\review_rerun"
echo.
echo  Running full honest eval...
echo  Out: %OUT%
"%EXE%" --eval --out "%OUT%"
echo.
echo  Done. See SUMMARY.md / VERDICT.md under:
echo  %OUT%
pause
goto menu

:do_gk
call :ensure_exe
if errorlevel 1 goto menu
set "OUT=%OUT_ROOT%\gk_loo"
echo  Running GK LOO...
"%EXE%" --gk-loo --out "%OUT%"
echo  Out: %OUT%
pause
goto menu

:do_smoke
call :ensure_exe
if errorlevel 1 goto menu
set "OUT=%OUT_ROOT%\smoke"
echo  Running smoke...
"%EXE%" --smoke --out "%OUT%"
echo  Out: %OUT%
pause
goto menu

:do_teacher
call :ensure_exe
if errorlevel 1 goto menu
set "OUT=%OUT_ROOT%\teacher_diag"
echo  Running teacher-diag...
"%EXE%" --teacher-diag --out "%OUT%"
echo  Out: %OUT%
pause
goto menu

:do_gui
call :ensure_exe
if errorlevel 1 goto menu
echo  Opening GUI...
start "" "%EXE%"
goto menu

:ensure_exe
if exist "%EXE%" exit /b 0
echo  EXE missing — building first...
call :build
if not exist "%EXE%" (
  echo [ERROR] Build did not produce: %EXE%
  pause
  exit /b 1
)
exit /b 0

:build
echo.
echo  MSBuild: %MSBUILD%
echo  Building Debug...
"%MSBUILD%" "%SLN%" /p:Configuration=Debug /v:m /nologo
if errorlevel 1 (
  echo [ERROR] Build failed.
  pause
  exit /b 1
)
echo  Build OK: %EXE%
exit /b 0

:find_msbuild
set "MSBUILD="
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
  exit /b 0
)
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
  exit /b 0
)
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
  exit /b 0
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  exit /b 0
)
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  exit /b 0
)
where msbuild >nul 2>&1
if not errorlevel 1 (
  for /f "delims=" %%i in ('where msbuild') do (
    set "MSBUILD=%%i"
    exit /b 0
  )
)
exit /b 1