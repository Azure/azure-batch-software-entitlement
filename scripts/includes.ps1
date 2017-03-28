# 
# Utility functions for scripts
#

# Write a divider line
function Write-Divider {
    Write-Host ("-" * $host.UI.RawUI.WindowSize.Width) -ForegroundColor DarkGray
}

# Write a header
function Write-Header($header) {
    Write-Host
    Write-Divider
    Write-Host $header -ForegroundColor Green
    Write-Divider
}
