@echo off
setlocal
pushd "%~dp0..\..\"
set "PYTHONUTF8=1"
set "HF_HUB_DISABLE_IMPLICIT_TOKEN=1"
set "HF_HUB_DISABLE_TELEMETRY=1"
set "AI_RESULT=0"
if not exist ".ml-runtime\python\python.exe" (
    echo Read AI\ClothData\README.md to set up the local Python environment.
    set "AI_RESULT=1"
    goto finish
)
".ml-runtime\python\python.exe" "AI\ClothData\collect_data.py"
if errorlevel 1 goto failed
".ml-runtime\python\python.exe" "AI\ClothData\prepare_data.py"
if errorlevel 1 goto failed
if not exist "AI\ClothData\Data\Processed\Contact\neutral_body_contact.npz" (
    ".ml-runtime\python\python.exe" "AI\ClothData\build_contact_data.py"
    if errorlevel 1 goto failed
)
".ml-runtime\python\python.exe" "AI\ClothData\verify_data.py"
if errorlevel 1 goto failed
echo Collection ready. Read AI\ClothData\COLLECTION_REPORT.md and verification.json.
goto finish
:failed
set "AI_RESULT=1"
echo A collection or verification step failed. Existing verified files are retained.
:finish
popd
pause
exit /b %AI_RESULT%
