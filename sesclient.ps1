#
# Convenience Launcher for sesclient
#

$sesclient = "$PSScriptRoot\x64\Debug\sesclient.native.exe"

# Uncomment this line to show which command is being run
# Write-Output "Launching $sesclient $args"

& $sesclient $args
