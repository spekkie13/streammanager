@echo off
REM Live Attack Spotlight selector. Usage: set-spotlight.bat home:3 | away:2 | off
if "%BOT_BASE_DIR%"=="" (set "BASE=%USERPROFILE%\Desktop\SpekkieTwitchBot") else (set "BASE=%BOT_BASE_DIR%")
<nul set /p "=%~1" > "%BASE%\Settings\spotlight-selection.txt"
