@echo off
call "%~1\VC\Auxiliary\Build\vcvarsall.bat" x86 >nul
if errorlevel 1 exit /b 1
cd /d "%~dp0"
if not exist bin mkdir bin
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc /LD Replay.cpp /Fobin\Replay.obj /Febin\AtroxReplay.dll /link user32.lib /DEF:Replay.def /DYNAMICBASE /NXCOMPAT >bin\build.log 2>&1
if errorlevel 1 (type bin\build.log & exit /b 1)
type bin\build.log
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc ..\tests\replay-native.cpp /Fobin\ReplayTest.obj /Febin\ReplayTest.exe /link /DYNAMICBASE /NXCOMPAT >bin\test-build.log 2>&1
if errorlevel 1 (type bin\test-build.log & exit /b 1)
bin\ReplayTest.exe
if errorlevel 1 exit /b 1
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc ..\tests\gameplay-native.cpp /Fobin\GameplayTest.obj /Febin\GameplayTest.exe /link /DYNAMICBASE /NXCOMPAT
if errorlevel 1 exit /b 1
bin\GameplayTest.exe
if errorlevel 1 exit /b 1
cl /nologo /std:c++17 /O2 /MT /W4 /EHsc ..\tests\saved-map-native.cpp /Fobin\SavedMapTest.obj /Febin\SavedMapTest.exe
if errorlevel 1 exit /b 1
bin\SavedMapTest.exe
exit /b %errorlevel%
