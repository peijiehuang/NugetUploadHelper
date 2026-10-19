@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem Always clean relative to this script's directory, not the current terminal directory.
set "ROOT=%~dp0"
set /a DELETED_COUNT=0
set /a FAILED_COUNT=0

echo ============================================================
echo Cleaning all bin and obj directories under:
echo %ROOT%
echo ============================================================

for /d /r "%ROOT%" %%D in (bin obj) do (
    if exist "%%~fD\" (
        echo [DELETE] %%~fD
        rd /s /q "%%~fD"

        if exist "%%~fD\" (
            echo [FAILED] %%~fD
            set /a FAILED_COUNT+=1
        ) else (
            set /a DELETED_COUNT+=1
        )
    )
)

echo.
echo ============================================================
echo Clean completed.
echo Deleted: !DELETED_COUNT!
echo Failed : !FAILED_COUNT!
echo ============================================================

if !FAILED_COUNT! GTR 0 (
    echo Some directories could not be removed.
    echo Close Visual Studio and other build processes, then try again.
)

if /i not "%~1"=="--no-pause" pause

exit /b !FAILED_COUNT!
