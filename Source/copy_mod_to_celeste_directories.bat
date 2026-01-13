@echo off
setlocal EnableDelayedExpansion

:: Source mod zip
set "SOURCE=D:\ReverseEngineering\.NET\CelesteRLAgentBridge\CelesteRLAgentBridge.zip"

:: Base Celeste install path
set "BASE=C:\Program Files (x86)\Steam\steamapps\common"

echo Copying CelesteRLAgentBridge mod to multiple instances...
echo Source: %SOURCE%
echo.

set INSTANCE_COUNT=8

:: Calculate N-1 because the loop is inclusive
set /a MAX_INDEX=%INSTANCE_COUNT% - 1

:: Loop over instances
for /L %%I in (0, 1, %MAX_INDEX%) do (
    set "DEST=!BASE!\Celeste%%I\Mods\CelesteRLAgentBridge.zip"
    
    :: Create Mods directory if it doesn't exist
    if not exist "!BASE!\Celeste%%I\Mods" (
        echo Creating directory: !BASE!\Celeste%%I\Mods
        mkdir "!BASE!\Celeste%%I\Mods"
    )
    
    :: Copy the file (overwrite if exists)
    echo Copying to Celeste%%I ...
    copy /Y "%SOURCE%" "!DEST!" >nul
    
    if errorlevel 1 (
        echo ERROR: Failed to copy to Celeste%%I
    ) else (
        echo SUCCESS: Copied to Celeste%%I
    )
    echo.
)

echo.
echo All done!
pause
