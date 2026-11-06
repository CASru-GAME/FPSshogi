@echo off
setlocal
set "FPS_SHOGI_EDITOR=%UNITY_EDITOR_PATH%"
if not defined FPS_SHOGI_EDITOR set "FPS_SHOGI_EDITOR=%ProgramFiles%\Unity\Hub\Editor\6000.2.12f1\Editor\Unity.exe"
if not exist "%FPS_SHOGI_EDITOR%" if exist "%USERPROFILE%\Documents\creategame\6000.2.12f1\Editor\Unity.exe" set "FPS_SHOGI_EDITOR=%USERPROFILE%\Documents\creategame\6000.2.12f1\Editor\Unity.exe"
if not exist "%FPS_SHOGI_EDITOR%" (
  echo Open this folder in Unity Hub with Unity 6000.2.12f1.
  echo Or set UNITY_EDITOR_PATH to the full path of Unity.exe.
  pause
  exit /b 1
)
start "" "%FPS_SHOGI_EDITOR%" -projectPath "%~dp0." -force-d3d11
