@ECHO OFF

ECHO Clearing Pacakges Directory
IF EXIST Packages (RMDIR Packages /s /q) 
MKDIR Packages

ECHO Packaging
nuget pack Microsoft.IoT.DeviceCore.nuspec -OutputDirectory Packages
nuget pack Microsoft.IoT.DeviceHelpers.nuspec -OutputDirectory Packages
nuget pack Microsoft.IoT.Devices.nuspec -OutputDirectory Packages
