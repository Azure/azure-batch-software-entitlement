#
# Build the cross-platform parts of the SDK using dotnet.exe
#
. $PSScriptRoot\scripts\includes.ps1

$dotnetExe = (get-command dotnet).Path
Write-Host "Dotnet executable: $dotnetExe"

Write-Header "Restoring Nuget packages"
& $dotnetExe restore .\src\sestest

Write-Header "Building SESTEST"
& $dotnetExe build .\src\sestest

Write-Header "Running Unit Tests"
foreach ($project in (resolve-path .\tests\*\*.csproj))
{
    & $dotnetExe test $project
}
