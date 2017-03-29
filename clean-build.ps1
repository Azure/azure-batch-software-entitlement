#
# Clean out all files from the .\out\ build artifacts directory
#
. $PSScriptRoot\scripts\includes.ps1
$outFolder = resolve-path $PSScriptRoot\out
Write-Header "Cleaning $outFolder"
remove-item $outFolder\* -recurse -ErrorAction SilentlyContinue 
