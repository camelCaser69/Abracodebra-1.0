@echo off
setlocal
title Unity Ultimate Extractor Launcher

:: Check if Python is installed/in PATH
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python is not found in your PATH.
    echo Please install Python 3 and check "Add Python to PATH" during installation.
    pause
    exit /b
)

:MENU
cls
echo ========================================================
echo   UNITY ULTIMATE EXTRACTOR
echo ========================================================
echo.
echo   1. Run ALL active profiles (Default)
echo   2. Extract SCRIPTS only
echo   3. Extract UI only
echo   4. List available profiles
echo   5. Exit
echo.
echo ========================================================
set /p option="Select an option [1-5]: "

if "%option%"=="1" goto RUN_ALL
if "%option%"=="2" goto RUN_SCRIPTS
if "%option%"=="3" goto RUN_UI
if "%option%"=="4" goto LIST_PROFILES
if "%option%"=="5" goto EXIT
goto MENU

:RUN_ALL
cls
echo Running ALL enabled profiles...
python unity_extractor.py
echo.
pause
goto MENU

:RUN_SCRIPTS
cls
echo Extracting SCRIPTS profile...
python unity_extractor.py --profile scripts
echo.
pause
goto MENU

:RUN_UI
cls
echo Extracting UI profile...
python unity_extractor.py --profile ui
echo.
pause
goto MENU

:LIST_PROFILES
cls
python unity_extractor.py --list
echo.
pause
goto MENU

:EXIT
exit /b