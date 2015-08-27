@ECHO OFF

REM Variables
SET MSBUILD="%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe"

REM Build All Projects
%MSBUILD% /v:m Build.proj

ECHO Copying Package Content to Builds
XCOPY /Y Content\*.* /s Builds
