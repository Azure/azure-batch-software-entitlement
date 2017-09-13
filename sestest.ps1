#
# Convenience Launcher for sestest
#

$sestest = "$PSScriptRoot\out\sestest\netcoreapp2.0\sestest.dll"

# Uncomment this line to show which exe is being run
# Write-Output "Launching $sestest"

dotnet $sestest $args