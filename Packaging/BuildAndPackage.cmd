@ECHO OFF

REM Variables
SET MSBUILD="%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe"

REM Build All Projects
%MSBUILD% /v:m Build.proj

ECHO Packaging
nuget pack Microsoft.IoT.Devices.nuspec
