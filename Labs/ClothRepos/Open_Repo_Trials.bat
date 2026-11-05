@echo off
cd /d "%~dp0"
set "TRIAL_PYTHON=%~dp0.venv\Scripts\python.exe"
if not exist "%TRIAL_PYTHON%" (
  echo First complete the Python and source setup in README.md.
  pause
  exit /b 1
)
"%TRIAL_PYTHON%" -X utf8 "%~dp0launch_trials.py"
if errorlevel 1 pause
