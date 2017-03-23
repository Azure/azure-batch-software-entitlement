function write-line {
    Write-Host ("-" * 80) -ForegroundColor Gray
}

function write-header($header) {
    write-line
    write-host $header
    write-line
}

write-line
$dotnet = resolve-path "C:\Program Files\dotnet\dotnet.exe"
Write-Host "Dot net runtime:             $dotnet"

$openCover = resolve-path $env:userprofile\.nuget\packages\OpenCover\*\tools\OpenCover.Console.exe 
Write-Host "Opencover executable:        $openCover"

$reportGenerator = resolve-path $env:userprofile\.nuget\packages\reportgenerator\*\tools\ReportGenerator.exe
Write-Host "Report Generator executable: $reportGenerator"
write-line

$commonTests = resolve-path .\tests\Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests\*.csproj
$sesTests = resolve-path .\tests\Microsoft.Azure.Batch.SoftwareEntitlement.Tests\*.csproj
$logLevel = "info"

$filter = "+[*]* -[xunit.*]* -[Fluent*]* -[*.Tests]* -[sestest]*"

Write-Header "Running tests for: $commonTests"
& $openCover -oldStyle -target:$dotnet -targetargs:"test $commonTests" -register:user -filter:$filter -log:$loglevel -output:.\out\Common.cover.xml

Write-Header "Running tests for $sesTests"
& $openCover -oldStyle -target:$dotnet -targetargs:"test $sesTests" -register:user -filter:$filter -log:$loglevel -output:.\out\Ses.cover.xml

Write-Header "Generating Report"
& $reportGenerator "-reports:.\out\Common.cover.xml;.\out\Ses.cover.xml" "-targetdir:.\out\cover\"

$reportIndex = resolve-path .\out\cover\index.htm
write-line
Write-Host "Test coverage report file:   $reportIndex"

& $reportIndex
