#
# Convenience Launcher for sestest
#

# Find the latest version of sestest that was compiled
# [This will automatically find the right version, even if someone does a release 
# build or if the output path changes e.g. becuase it's no longer netcoreapp2.0]
$sestest = get-childitem $PSScriptRoot\out\sestest.dll -recurse | sort-object LastWriteTimeUtc | select-object -last 1

if ($sesclient -eq $null) 
{
    Write-Host "Could not find 'sestest.dll' within $PSScriptRoot\x64" -ForegroundColor Red
    Write-Host "Do you need to run a build?" -ForegroundColor Blue
    exit -1
}


# Uncomment this line to show which exe is being run
# Write-Output "Launching $sestest"

dotnet $sestest $args
