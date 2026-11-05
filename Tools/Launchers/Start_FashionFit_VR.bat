@echo off
setlocal
set "FASHIONFIT_ROOT=%~dp0..\.."
set "FASHIONFIT_META=%ProgramFiles%\Meta Horizon\Support\oculus-client\OculusClient.exe"
if not exist "%FASHIONFIT_META%" set "FASHIONFIT_META=%ProgramFiles%\Oculus\Support\oculus-client\OculusClient.exe"
if not exist "%FASHIONFIT_ROOT%\Open_FashionFit_Model.bat" goto missing
if /i "%~1"=="--check" goto check
if exist "%FASHIONFIT_META%" start "Meta Horizon Link" "%FASHIONFIT_META%"
call "%FASHIONFIT_ROOT%\Open_FashionFit_Model.bat"
exit /b %errorlevel%
:check
call "%USERPROFILE%\Downloads\FashionFit_Run\Open_Model.bat" --check
if errorlevel 1 exit /b 1
if not exist "%FASHIONFIT_META%" exit /b 1
echo Full game, Meta Link and source launcher are ready.
exit /b 0
:missing
echo FashionFit source launcher is missing. See START_HERE.txt in the project.
pause
exit /b 1
