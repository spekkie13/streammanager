@echo off
if "%BOT_BASE_DIR%"=="" (set "BASE=%USERPROFILE%\Desktop\SpekkieTwitchBot") else (set "BASE=%BOT_BASE_DIR%")
<nul set /p "=away:1" > "%BASE%\Settings\spotlight-selection.txt"
