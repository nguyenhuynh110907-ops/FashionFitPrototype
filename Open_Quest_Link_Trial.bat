@echo off
setlocal
pushd "%~dp0"
if not exist "Assets\FashionFit\Scenes\FashionFitPrototype.unity" goto missing_project
set "FASHIONFIT_PROJECT_DIR=%CD%"
set "META_EXE=%ProgramFiles%\Meta Horizon\Support\oculus-client\OculusClient.exe"
if not exist "%META_EXE%" set "META_EXE=%ProgramFiles%\Oculus\Support\oculus-client\OculusClient.exe"
if exist "%META_EXE%" start "" "%META_EXE%"
set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" goto missing_unity
if not exist "Logs" mkdir "Logs"
powershell -NoProfile -Command "$p=Join-Path $env:FASHIONFIT_PROJECT_DIR 'Temp\UnityLockfile'; if(Test-Path -LiteralPath $p){try{$s=[System.IO.File]::Open($p,'Open','ReadWrite','None');$s.Dispose()}catch{exit 10}};exit 0"
if errorlevel 10 goto already_open
start "Fashion Fit Quest Link Trial" "%UNITY_EXE%" -projectPath "%FASHIONFIT_PROJECT_DIR%" -executeMethod FashionFit.Editor.QuestLinkTrialLauncher.OpenAndRun -logFile "%FASHIONFIT_PROJECT_DIR%\Logs\QuestLink-Trial.log"
popd
exit /b 0
:already_open
echo Unity is already open. Use the menu:
echo Fashion Fit - Prepare Quest Link Trial
echo Connect the headset and launch Link inside it, then click Enter VR in Game.
pause
popd
exit /b 0
:missing_unity
echo Unity 2022.3.62f1 was not found. Open this project in Unity Hub,
echo then select Fashion Fit - Prepare Quest Link Trial.
pause
popd
exit /b 1
:missing_project
echo Keep this file in the FashionFitPrototype project folder.
pause
popd
exit /b 1
