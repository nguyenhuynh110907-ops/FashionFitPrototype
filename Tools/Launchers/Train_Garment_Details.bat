@echo off
setlocal
cd /d "%~dp0..\..\"
set PYTHONUTF8=1
set HF_HUB_OFFLINE=1
set "DETAIL_PY=%~dp0..\..\.ml-runtime\python\python.exe"
if not exist "%DETAIL_PY%" (
  echo Local Python is missing. See AI\GarmentDetail\DETAIL_TRAINING_REPORT.md
  pause
  exit /b 1
)
"%DETAIL_PY%" AI\GarmentDetail\prepare_detail_regions.py
if errorlevel 1 goto failed
"%DETAIL_PY%" AI\GarmentDetail\train_detail_regions.py --epochs 30
if errorlevel 1 goto failed
"%DETAIL_PY%" AI\GarmentDetail\train_pattern_details.py
if errorlevel 1 goto failed
"%DETAIL_PY%" AI\GarmentDetail\verify_detail_pipeline.py
if errorlevel 1 goto failed
echo Offline training and data verification finished. Read AI\GarmentDetail\DETAIL_TRAINING_REPORT.md
pause
exit /b 0
:failed
echo Failed. Read the error above; existing source garments were not modified.
pause
exit /b 1
