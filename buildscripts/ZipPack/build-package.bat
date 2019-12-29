rem Generate the zip Release package.
rem Highly recommend to use a Continuous Integration service to pull source codes from repository, build, test, package and deploy

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
xcopy /y "%initialWD%\%TargetName%.dll" %rootPath%GameData\%TargetName%\Plugins\%TargetName%.dll*

IF EXIST %rootPath%package\ rd /s /q %rootPath%package
mkdir %rootPath%package\GameData
cd %rootPath%package\GameData

echo Copying %rootPath%GameData files to package stage
xcopy /y /E /Q %rootPath%GameData %rootPath%package\GameData

echo.
echo Compressing %TargetName% for %KSPversion% Release Package...
IF EXIST "%rootPath%%TargetName%*_For_%KSPversion%.zip" del "%rootPath%%TargetName%*_For_%KSPversion%.zip"
"%scriptPath%7za.exe" a "%rootPath%%TargetName%%Dllversion%_For_%KSPversion%.zip" %rootPath%package\GameData

echo Deleting package stage
rd /s /q %rootPath%package
