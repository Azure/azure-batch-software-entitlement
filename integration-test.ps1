## ---------------------------------------------------
##   Complete end to end integration test of the SDK
## ---------------------------------------------------

param(
    # A certificate that can be used to secure the token and our communication
    [Parameter(Mandatory=$true, HelpMessage="Thumbprint of the certificate to use for HTTP/TLS, for encryption, and for signing.")]
    [string]$thumbprint = $null,

    # common name of the certificate specified by $thumbprint
    [string]$commonName = "localhost",

    # URL of the local sestest server 
    [string]$url = "https://localhost:4443"
)

#
## NOTE: the server name from $url MUST MATCH (allowing for wildcards) the common name of the certificate
## So if the URL is for "localhost" the certificate common name must also specify "localhost"

if ($thumbprint -eq $null) {
    Write-Host "Please define $thumbprint to identify which certificate to use for the test."
    exit -1;
}

function Write-TaskName($subtaskName) {
    $divider = "-" * ($subtaskName.Length + 4)
    Write-Host "`r`n$divider`r`n  $subtaskName`r`n$divider"
}

# ----------------------------------------------------------------------

Write-TaskName "Generating Token"

.\sestest.ps1 generate --vmid "fu" --application-id contosoapp --sign $thumbprint --encrypt $thumbprint --token-file token.txt

if ($LASTEXITCODE -ne 0) {
    Write-Output "Failed to generate token (Error code $LASTEXITCODE)"
    exit
}

# ----------------------------------------------------------------------

Write-TaskName "Display Token"

$env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN = (get-content token.txt)
Write-Host $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN

# ----------------------------------------------------------------------

Write-TaskName "Start Software Entitlement Server"

start-process powershell -argument ".\sestest.ps1 server --connection $thumbprint --sign $thumbprint --encrypt $thumbprint --exit-after-request"


# ----------------------------------------------------------------------

Write-TaskName "Wait 5s for server to be up"

start-sleep 5

# ----------------------------------------------------------------------

Write-TaskName "Verify Token"

.\sesclient --url $url --thumbprint $thumbprint --common-name $commonName --token $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN --application contosoapp

if ($LASTEXITCODE -ne 0) {
    Write-Output "Token did not verify (Error code $LASTEXITCODE)"
} else {
    Write-Output "Token verified ok"
}
