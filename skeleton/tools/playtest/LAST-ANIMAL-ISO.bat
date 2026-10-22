@echo off
rem Last Animal - owner playtest launcher (MC 11129).
rem ISO rig = the FIXED ISOMETRIC camera. The -- tells Godot: everything after
rem it is a GAME flag, not an engine flag; the game reads --iso-cam itself.
rem The boot line "LA_PLAYTEST_RIG=ISO" below names the rig that is live.
rem Keep this .bat beside LastAnimal_console.exe + the data_ folder: the game
rem package must stay together.
cd /d "%~dp0"
LastAnimal_console.exe -- --iso-cam
echo.
echo Game closed. This session ran the ISO rig (see the LA_PLAYTEST_RIG=
echo line above). Press any key to close this window.
pause >nul
