@ECHO OFF

REM Variables
SET MSBUILD="%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe"

REM Build All Projects
%MSBUILD% /v:m Build.proj

REM Clear Packages directory
IF EXIST Packages (RMDIR Packages /s /q) 
MKDIR Packages

ECHO Copying Package Content
XCOPY Content\*.* /s Builds

ECHO Packaging
nuget pack Microsoft.IoT.Devices.nuspec -OutputDirectory Packages
