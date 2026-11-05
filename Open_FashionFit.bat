@echo off
setlocal
title FashionFit - Unity Editor
pushd "%~dp0"
set "FASHIONFIT_PROJECT_DIR=%CD%"
if not exist "Assets\FashionFit\Scenes\FashionFitPrototype.unity" goto missing_project
set "FASHIONFIT_UNITY=%ProgramFiles%\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe"
if not exist "%FASHIONFIT_UNITY%" goto missing_unity
if not exist "Logs" mkdir "Logs"
powershell -NoProfile -Command "$f=Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -and $_.CommandLine.IndexOf($env:FASHIONFIT_PROJECT_DIR,[System.StringComparison]::OrdinalIgnoreCase) -ge 0}; if($f){exit 10}; exit 0"
if errorlevel 10 goto already_open
echo Opening Unity 2022.3.62f1. Wait for package import and script compilation.
echo For the ready-to-run model, use Open_FashionFit_Model.bat instead.
start "FashionFit Editor" "%FASHIONFIT_UNITY%" -projectPath "%FASHIONFIT_PROJECT_DIR%" -logFile "%FASHIONFIT_PROJECT_DIR%\Logs\FashionFit-Editor.log"
popd
exit /b 0
:already_open
echo This project is already open in Unity. Switch to that window.
echo Use Fashion Fit - Open and Run Prototype if Play is stopped.
pause
popd
exit /b 0
:missing_unity
echo Unity 2022.3.62f1 was not found. Select this version in Unity Hub.
echo The exported player can be opened with Open_FashionFit_Model.bat.
pause
popd
exit /b 1
:missing_project
echo Keep this launcher inside the FashionFitPrototype source project.
pause
popd
exit /b 1
