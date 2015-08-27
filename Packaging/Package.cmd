@ECHO OFF

ECHO Packaging
nuget pack Microsoft.IoT.DeviceCore.nuspec -OutputDirectory Packages
nuget pack Microsoft.IoT.DeviceHelpers.nuspec -OutputDirectory Packages
nuget pack Microsoft.IoT.Devices.nuspec -OutputDirectory Packages
