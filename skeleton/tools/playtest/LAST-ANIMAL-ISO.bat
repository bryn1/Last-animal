@echo off
rem Last Animal - owner playtest launcher (MC 11129).
rem ISO rig = the FIXED ISOMETRIC camera. The -- tells Godot: everything after
rem it is a GAME flag, not an engine flag; the game reads --iso-cam itself.
rem The game prints one line at boot naming the rig. It is saved into
rem BOOT-LOG-ISO.txt in this folder and shown here when you close the game.
rem Keep this .bat beside LastAnimal.exe + the data_ folder (one package).
cd /d "%~dp0"
powershell -NoProfile -Command "Start-Process -FilePath '%~dp0LastAnimal.exe' -ArgumentList '--','--iso-cam' -RedirectStandardOutput '%~dp0BOOT-LOG-ISO.txt' -RedirectStandardError '%~dp0BOOT-LOG-ISO-err.txt' -Wait"
echo.
echo This session ran the ISO rig. Its boot line:
findstr /i "LA_PLAYTEST_RIG" "BOOT-LOG-ISO.txt"
echo.
echo (Full log: BOOT-LOG-ISO.txt in this folder.)
echo Press any key to close this window.
pause >nul
