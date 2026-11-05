@echo off
setlocal
pushd "%~dp0..\..\"
if not exist ".ml-runtime\python\python.exe" (
    echo Read AI\FitVision\README.md to set up the local training environment.
    popd
    pause
    exit /b 1
)
set "PYTHONUTF8=1"
set "HF_HUB_OFFLINE=1"
set "HF_HUB_DISABLE_TELEMETRY=1"
".ml-runtime\python\python.exe" "AI\FitVision\fitvision.py" train
set "AI_RESULT=%ERRORLEVEL%"
popd
pause
exit /b %AI_RESULT%
