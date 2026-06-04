@echo off
if "%BOT_BASE_DIR%"=="" (set "BASE=%USERPROFILE%\Desktop\SpekkieTwitchBot") else (set "BASE=%BOT_BASE_DIR%")
<nul set /p "=off" > "%BASE%\Settings\spotlight-selection.txt"
