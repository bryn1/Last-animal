@echo off
rem Last Animal - owner playtest launcher (MC 11129; default flipped MC 11189).
rem CLASSIC rig = the FOLLOW camera. It is NOT the default anymore (owner
rem ruling "Iso,"): this launcher opts OUT of the default with the session-only
rem --classic-cam game flag. The -- tells Godot: everything after
rem it is a GAME flag, not an engine flag; the game reads --classic-cam itself.
rem The game prints one line at boot naming the rig. It is saved into
rem BOOT-LOG-CLASSIC.txt in this folder and shown here when you close the game.
rem Keep this .bat beside LastAnimal.exe + the data_ folder (one package).
cd /d "%~dp0"
powershell -NoProfile -Command "Start-Process -FilePath '%~dp0LastAnimal.exe' -ArgumentList '--','--classic-cam' -RedirectStandardOutput '%~dp0BOOT-LOG-CLASSIC.txt' -RedirectStandardError '%~dp0BOOT-LOG-CLASSIC-err.txt' -Wait"
echo.
echo This session ran the CLASSIC rig. Its boot line:
findstr /i "LA_PLAYTEST_RIG" "BOOT-LOG-CLASSIC.txt"
echo.
echo (Full log: BOOT-LOG-CLASSIC.txt in this folder.)
echo Press any key to close this window.
pause >nul
