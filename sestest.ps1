#
# Convenience Launcher for sestest
#

$sestest = "$PSScriptRoot\out\sestest\Debug\netcoreapp1.1\sestest.dll"

# Uncomment this line to show which exe is being run
# Write-Output "Launching $sestest"

dotnet $sestest $args