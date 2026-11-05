@echo off
setlocal
set "FASHIONFIT_TRIAL=%USERPROFILE%\Downloads\FashionFit_Run\FashionFittingRoom.exe"
if not exist "%FASHIONFIT_TRIAL%" (
  echo FashionFit player was not found at "%FASHIONFIT_TRIAL%".
  pause
  exit /b 1
)
start "FashionFit - Button blouse trial" "%FASHIONFIT_TRIAL%" -fashionDoffTrial
endlocal
