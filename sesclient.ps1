#
# Convenience Launcher for sesclient
#

$sesclient = "$PSScriptRoot\x64\Debug\sesclient.native.exe"

# Uncomment this line to show which exe is being run
# Write-Output "Launching $sesclient"

& $sesclient $args
