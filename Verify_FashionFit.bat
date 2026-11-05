@echo off
setlocal
set "FASHIONFIT_PY=%~dp0.ml-runtime\python\python.exe"
if exist "%FASHIONFIT_PY%" goto bundled
py -3 -X utf8 "%~dp0Tools\Studio\verify.py" %*
goto done
:bundled
"%FASHIONFIT_PY%" -X utf8 "%~dp0Tools\Studio\verify.py" %*
:done
set "FASHIONFIT_CODE=%errorlevel%"
if "%~1"=="" pause
exit /b %FASHIONFIT_CODE%
