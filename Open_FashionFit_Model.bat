@echo off
setlocal
set "FASHIONFIT_RUN=%USERPROFILE%\Downloads\FashionFit_Run"
if not exist "%FASHIONFIT_RUN%\Open_Model.bat" goto missing
call "%FASHIONFIT_RUN%\Open_Model.bat" %*
exit /b %errorlevel%
:missing
echo The local Windows player is missing:
echo %FASHIONFIT_RUN%
echo To edit the Unity project, open Open_FashionFit.bat in the project folder.
pause
exit /b 1
