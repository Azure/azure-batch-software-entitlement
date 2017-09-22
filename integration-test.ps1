## ---------------------------------------------------
##   Complete end to end integration test of the SDK
## ---------------------------------------------------

## How to configure this script
## ----------------------------

## 1. Select a certificate to use and put its thumbprint into $thumbprint:
$thumbprint = "<thumbprint>"

## 2. Update $url to be a valid name for this machine (must be https on port 4443):
$url = "https://localhost:4443"

## 3. Find the common name from the certificate and update $commonName
$commonName = "localhost"

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

