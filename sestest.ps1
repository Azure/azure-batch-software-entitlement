#
# Convenience Launcher for sestest
#

# Find the latest version of sestest that was compiled
# [This will automatically find the right version, even if someone does a release 
# build or if the output path changes e.g. becuase it's no longer netcoreapp2.0]
$sestest = get-childitem $PSScriptRoot\out\sestest\sestest.dll -recurse | sort-object LastWriteTimeUtc | select-object -last 1

if ($sestest -eq $null) 
{
    Write-Host "Could not find 'sestest.dll' within $PSScriptRoot\out" -ForegroundColor Red
    Write-Host "Do you need to run a build?" -ForegroundColor Yellow
    exit -1
}


# Uncomment this line to show which exe is being run
# Write-Output "Launching $sestest"

dotnet $sestest $args
