@echo off
setlocal
pushd "%~dp0..\..\"
set "PYTHONUTF8=1"
set "DETAIL_RESULT=0"
if not exist ".ml-runtime\python\python.exe" goto missing
if not exist "AI\ClothData\Data\Processed\garments.sqlite3" goto missing
".ml-runtime\python\python.exe" "AI\GarmentDetail\prepare_training.py"
if errorlevel 1 goto failed
echo Prepared pattern-aware pilot graphs and the Unity inspection sample.
echo Open Fashion Fit ^> AI ^> Inspect Detailed Garment in Unity.
echo Read AI\GarmentDetail\README.md before training.
goto finish
:missing
echo Local Python or ClothData is missing. Read AI\ClothData\README.md.
goto failed
:failed
set "DETAIL_RESULT=1"
echo Preparation failed. Read the error above. No new download was started.
:finish
popd
pause
exit /b %DETAIL_RESULT%
