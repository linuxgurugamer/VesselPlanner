@echo off
setlocal
if "%~1"=="" (
  echo Usage: BuildRelease.bat "C:\Path\To\Kerbal Space Program"
  exit /b 1
)
set "KSP_ROOT=%~1"
msbuild EngineStagePlanner.sln /p:Configuration=Release /p:KSP_ROOT="%KSP_ROOT%" /p:CopyToKSP=true
endlocal
