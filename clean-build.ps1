#
# Clean out all files from the .\out\ build artifacts directory
#
. $PSScriptRoot\scripts\includes.ps1

$srcFolder = resolve-path $PSScriptRoot\src
$testsFolder = resolve-path $PSScriptRoot\tests
$outFolder = resolve-path $PSScriptRoot\out

Write-Header "Cleaning"

Write-Output $srcFolder.Path
remove-item $srcFolder\*\obj\* -recurse -ErrorAction SilentlyContinue

Write-Output $testsFolder.Path
remove-item $testsFolder\*\obj\* -recurse -ErrorAction SilentlyContinue

Write-Output $outFolder.Path
remove-item $outFolder\* -recurse -ErrorAction SilentlyContinue 

