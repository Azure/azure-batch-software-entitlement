#
# Convenience Launcher for sesclient
#

# Find the latest version of sesclient.native.exe that was compiled
# [This will automatically find the right version, even if someone does a release 
# build or if the output path changes]
$sesclient = get-childitem $PSScriptRoot\x64\sesclient.native.exe -recurse | sort-object LastWriteTimeUtc | select-object -last 1

if ($sesclient -eq $null) 
{
    Write-Host "Could not find 'sesclient.native.exe' within $PSScriptRoot\x64" -ForegroundColor Red
    Write-Host "Do you need to run a build?" -ForegroundColor Blue
    exit -1
}

# Uncomment this line to show which command is being run
# Write-Output "Launching $sesclient $args"

& $sesclient $args
