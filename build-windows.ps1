#
# Build native components for Windows
#
param (
    # Valid choices are: "x86" or "x64"
    $platform = "x64", 
    # Valid choices are: "Release" or "Debug"
    $configuration = "Debug"
)

. $PSScriptRoot\scripts\includes.ps1

$msbuildExe = resolve-path "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
Write-Host "MSBuild executable: $msbuildExe"

Write-Header "Build Native Library for Windows"
& $msbuildExe .\src\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\ /property:Configuration=$configuration /property:Platform=$platform

Write-Header "Build Native console tool for Windows"
& $msbuildExe .\src\sesclient.native\ /property:Configuration=$configuration /property:Platform=$platform
