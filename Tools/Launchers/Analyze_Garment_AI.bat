@echo off
setlocal
pushd "%~dp0..\..\"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "AI\FitVision\Predict-Image.ps1"
set "AI_RESULT=%ERRORLEVEL%"
popd
pause
exit /b %AI_RESULT%
