@echo off
call "%~1\VC\Auxiliary\Build\vcvarsall.bat" x86 >nul
if errorlevel 1 exit /b 1
cd /d "%~dp0"
if not exist bin mkdir bin
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc /LD Replay.cpp /Fobin\Replay.obj /Febin\AtroxReplay.dll /link /DEF:Replay.def /DYNAMICBASE /NXCOMPAT >bin\build.log 2>&1
if errorlevel 1 (type bin\build.log & exit /b 1)
type bin\build.log
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc ..\tests\replay-native.cpp /Fobin\ReplayTest.obj /Febin\ReplayTest.exe /link /DYNAMICBASE /NXCOMPAT >bin\test-build.log 2>&1
if errorlevel 1 (type bin\test-build.log & exit /b 1)
bin\ReplayTest.exe
exit /b %errorlevel%
