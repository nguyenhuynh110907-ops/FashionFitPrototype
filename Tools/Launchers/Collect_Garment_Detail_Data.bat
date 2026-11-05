@echo off
setlocal
cd /d "%~dp0..\..\"
set PYTHONUTF8=1
set "DETAIL_PY=%~dp0..\..\.ml-runtime\python\python.exe"
"%DETAIL_PY%" AI\GarmentDetail\collect_material_details.py
if errorlevel 1 goto failed
"%DETAIL_PY%" AI\GarmentDetail\collect_pattern_details.py
if errorlevel 1 goto failed
echo Collection complete. Run Train_Garment_Details.bat to prepare and train offline.
pause
exit /b 0
:failed
echo Collection failed. Verified files are kept; rerun to resume.
pause
exit /b 1
