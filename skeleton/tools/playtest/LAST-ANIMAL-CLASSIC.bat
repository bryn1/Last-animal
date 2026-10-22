@echo off
rem Last Animal - owner playtest launcher (MC 11129).
rem CLASSIC rig = the shipped FOLLOW camera (the default; no launch flag).
rem The game prints one line at boot naming the rig. It is saved into
rem BOOT-LOG-CLASSIC.txt in this folder and shown here when you close the game.
rem Keep this .bat beside LastAnimal.exe + the data_ folder (one package).
cd /d "%~dp0"
powershell -NoProfile -Command "Start-Process -FilePath '%~dp0LastAnimal.exe' -RedirectStandardOutput '%~dp0BOOT-LOG-CLASSIC.txt' -RedirectStandardError '%~dp0BOOT-LOG-CLASSIC-err.txt' -Wait"
echo.
echo This session ran the CLASSIC rig. Its boot line:
findstr /i "LA_PLAYTEST_RIG" "BOOT-LOG-CLASSIC.txt"
echo.
echo (Full log: BOOT-LOG-CLASSIC.txt in this folder.)
echo Press any key to close this window.
pause >nul
