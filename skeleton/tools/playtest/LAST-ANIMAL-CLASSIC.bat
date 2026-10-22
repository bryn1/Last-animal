@echo off
rem Last Animal - owner playtest launcher (MC 11129).
rem CLASSIC rig = the shipped FOLLOW camera (the default; no launch flag).
rem The boot line "LA_PLAYTEST_RIG=CLASSIC" below names the rig that is live.
rem Keep this .bat beside LastAnimal_console.exe + the data_ folder: the game
rem package must stay together.
cd /d "%~dp0"
LastAnimal_console.exe
echo.
echo Game closed. This session ran the CLASSIC rig (see the LA_PLAYTEST_RIG=
echo line above). Press any key to close this window.
pause >nul
