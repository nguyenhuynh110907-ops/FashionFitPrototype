@echo off
setlocal
pushd "%~dp0..\..\"
set "PYTHONUTF8=1"
set "DETAIL_RESULT=0"
if not exist ".ml-runtime\python\python.exe" goto missing
if not exist "AI\ClothData\Data\DetailTraining\pilot_manifest.json" goto missing
".ml-runtime\python\python.exe" "AI\GarmentDetail\train_drape.py" --dataset pilot --epochs 8
if errorlevel 1 goto failed
".ml-runtime\python\python.exe" "AI\GarmentDetail\verify_preparation.py"
if errorlevel 1 goto failed
echo Pilot trained and checked. This model is for research; it is not deployed into the game.
echo Read AI\GarmentDetail\verification.json and TRAINING_PREPARATION_REPORT.md.
goto finish
:missing
echo Python or pilot graphs missing. Run Prepare_Detailed_Garment_Training.bat first.
goto failed
:failed
set "DETAIL_RESULT=1"
echo A training or verification step failed. Read the error above.
:finish
popd
pause
exit /b %DETAIL_RESULT%
