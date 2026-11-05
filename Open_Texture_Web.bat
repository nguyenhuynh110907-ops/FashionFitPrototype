@echo off
setlocal
pushd "%~dp0"
if not exist "AI\MaterialLab\FabricViewer.html" goto missing
start "FashionFit Material Library" "AI\MaterialLab\FabricViewer.html"
popd
exit /b 0
:missing
echo Missing AI\MaterialLab\FabricViewer.html. Open this launcher from the project folder.
popd
pause
exit /b 1
