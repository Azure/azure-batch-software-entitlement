#
# Build the cross-platform parts of the SDK using dotnet.exe
#
. $PSScriptRoot\scripts\includes.ps1

$dotnetExe = resolve-path "C:\Program Files\dotnet\dotnet.exe"
Write-Host "Dotnet executable: $dotnetExe"

Write-Header "Restoring Nuget packages"
& $dotnetExe restore .\src\sestest

Write-Header "Building SESTEST"
& $dotnetExe build .\src\sestest

