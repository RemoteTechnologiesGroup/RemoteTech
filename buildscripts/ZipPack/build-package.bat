rem Generate the zip Release package.

@echo off

rem get parameters that are passed by visual studio post build event
SET TargetName=%1
SET Dllversion=%~n2
SET KSPversion=%3

rem make sure the initial working directory is the one containing the current script
SET scriptPath=%~dp0
SET rootPath=%scriptPath%..\..\
SET initialWD=%CD%

echo Generating %TargetName% for %KSPversion% Release Package...
cd "%rootPath%"
xcopy /y "%initialWD%\%TargetName%.dll" GameData\%TargetName%\Plugins\%TargetName%.dll*

IF EXIST package\ rd /s /q package
mkdir package
cd package

mkdir GameData
cd GameData

mkdir "%TargetName%"
cd "%TargetName%"
xcopy /y /e "..\..\..\GameData\%TargetName%\*" .
xcopy /y ..\..\..\CHANGES.md .
xcopy /y ..\..\..\LICENSE.txt .

echo.
echo Compressing %TargetName% for %KSPversion% Release Package...
IF EXIST "%rootPath%%TargetName%*_For_%KSPversion%.zip" del "%rootPath%%TargetName%*_For_%KSPversion%.zip"
"%scriptPath%7za.exe" a "..\..\..\%TargetName%%Dllversion%_For_%KSPversion%.zip" ..\..\GameData

cd "%rootPath%"
rd /s /q package

cd "%initialWD%"
