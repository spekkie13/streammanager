@echo off
if "%BOT_BASE_DIR%"=="" (set "BASE=%USERPROFILE%\Desktop\SpekkieTwitchBot") else (set "BASE=%BOT_BASE_DIR%")
<nul set /p "=home:5" > "%BASE%\Settings\spotlight-selection.txt"
