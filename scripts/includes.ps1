# 
# Utility functions for scripts
#

# Write a divider line
function Write-Divider {
    Write-Output ("-" * $host.UI.RawUI.WindowSize.Width)
}

# Write a header
function Write-Header($header) {
    Write-Output ""
    Write-Divider
    Write-Output $header
    Write-Divider
}
