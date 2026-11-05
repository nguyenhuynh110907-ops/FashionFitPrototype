@echo off
setlocal
pushd "%~dp0..\..\"
set "PYTHONUTF8=1"
set "DETAIL_RESULT=0"
if not exist ".ml-runtime\python\python.exe" goto failed
if not exist "AI\ClothData\Data\DetailTraining\pilot_manifest.json" goto missing
".ml-runtime\python\python.exe" "AI\GarmentDetail\prepare_surface_details.py"
if errorlevel 1 goto failed
".ml-runtime\python\python.exe" "AI\GarmentDetail\verify_surface_details.py"
if errorlevel 1 goto failed
echo Detail labels, sewing curves and source geometry checked.
echo Read AI\GarmentDetail\SURFACE_DETAIL_DATA.md. No model was deployed.
goto finish
:missing
echo Run Prepare_Detailed_Garment_Training.bat first.
:failed
set "DETAIL_RESULT=1"
echo Preparation or verification failed. Read the error above.
:finish
popd
pause
exit /b %DETAIL_RESULT%
