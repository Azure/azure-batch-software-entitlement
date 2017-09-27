##
##  Psake build file for the Software Entitlement Service SDK
##

properties {
    $baseDir = resolve-path ..\
    $srcDir = resolve-path $baseDir\src
    $testsDir = resolve-path $baseDir\tests
    $outDir = join-path $baseDir out
    $publishDir = join-path $baseDir publish
}

Task Clean -Depends Clean.SourceFolder, Clean.OutFolder, Clean.PublishFolder

Task Build.Xplat -Depends Build.SesTest, Unit.Tests

Task Publish.Archives -Depends Clean.PublishFolder, Publish.SesTest.Win64, Publish.SesTest.Linux64, Publish.SesClient

Task Build.Windows -Depends Build.SesLibrary, Build.SesClient

## --------------------------------------------------------------------------------
##   Preparation Targets
## --------------------------------------------------------------------------------
## Tasks used to prepare for the actual build

Task Restore.NuGetPackages -Depends Requires.DotNetExe {
    exec {
        & $dotnetExe restore $baseDir\src\sestest
    }
}

Task Clean.SourceFolder {
    remove-item $srcDir\*\obj\* -recurse -ErrorAction SilentlyContinue
    remove-item $testsDir\*\obj\* -recurse -ErrorAction SilentlyContinue
}

Task Clean.OutFolder {
    remove-item $outDir\* -recurse -ErrorAction SilentlyContinue
}

Task Clean.PublishFolder {
    remove-item $publishDir -Force -Recurse -ErrorAction SilentlyContinue
}

## --------------------------------------------------------------------------------
##   Build Targets
## --------------------------------------------------------------------------------
## Tasks used to perform steps of the actual build

Task Generate.Version {
    
    $script:versionBase = get-content $baseDir\version.txt -ErrorAction SilentlyContinue
    if ($versionBase -eq $null) {
        throw "Unable to load $baseDir\version.txt"
    }
    $versionLastUpdated = git rev-list -1 HEAD $baseDir\version.txt
    $script:patchVersion = git rev-list "$versionLastUpdated..HEAD" --count

    $script:version = "$versionBase.$patchVersion"
    Write-Host "Version          $version"

    $script:semanticVersion = $version
    Write-Host "Semantic version $semanticVersion"
}

Task Build.SesTest -Depends Requires.DotNetExe, Restore.NuGetPackages, Generate.Version {
    $project = resolve-path $srcDir\sestest\sestest.csproj
    Write-Host "Building $project"
    exec {
        & $dotnetExe build $project /property:Version=$semanticVersion /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\sestest.msbuild.log --no-restore
    }
}

Task Build.SesLibrary -Depends Requires.MsBuild, Requires.Configuration, Requires.Platform, Generate.Version {
    exec {
        & $msbuildExe $srcDir\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\ /p:Version=$version /property:Configuration=$configuration /property:Platform=$targetPlatform /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\seslibrary.msbuild.log
    }
}

Task Build.SesClient -Depends Requires.MsBuild, Requires.Configuration, Requires.Platform, Generate.Version {
    exec {
        & $msbuildExe $srcDir\sesclient.native\ /property:Configuration=$configuration /p:Version=$version /property:Platform=$targetPlatform /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\sesclient.msbuild.log
    }
}

Task Unit.Tests -Depends Requires.DotNetExe, Build.SesTest {
    foreach ($project in (resolve-path $testsDir\*\*.csproj)){
        Write-SubtaskName (split-path $project -Leaf)
        exec {
            & $dotnetExe test $project
        }
    }
}

Task Cover.Tests -Depends Requires.Opencover, Requires.ReportGenerator, Requires.OutDir, Build.SesTest {
    $filter = "+[*]* -[xunit.*]* -[Fluent*]* -[*.Tests]*"
    $logLevel = "info"

    foreach ($project in (resolve-path $testsDir\*\*.csproj)){
        $projectName = split-path $project -Leaf
        Write-SubtaskName $projectName

        exec {
            & $openCoverExe -oldStyle "-target:$dotnetExe" "-targetargs:test $project" -register:user "-filter:$filter" -log:$loglevel -output:$outDir\$projectName.cover.xml
        }
    }

    Write-SubtaskName "Generating Coverage Report"

    & $reportGeneratorExe "-reports:$outDir\*.cover.xml" "-targetdir:$outDir\cover\"
}

Task Publish.SesTest.Win64 -Depends Requires.DotNetExe, Restore.NuGetPackages {
    exec {
        & $dotnetExe publish $srcDir\sestest\sestest.csproj --self-contained --output $publishDir\sestest\win10-x64 --runtime win10-x64 /p:Version=$semanticVersion
    }

    exec {
        compress-archive $publishDir\sestest\win10-x64\* $publishDir\sestest-$version-win10-x64.zip 
    }
}

Task Publish.SesTest.Linux64 -Depends Requires.DotNetExe, Restore.NuGetPackages {
    exec {
        & $dotnetExe publish $srcDir\sestest\sestest.csproj --self-contained --output $publishDir\sestest\linux-x64 --runtime linux-x64 /p:Version=$semanticVersion
    }

    exec {
        compress-archive $publishDir\sestest\linux-x64\* $publishDir\sestest-$version-linux-x64.zip
    }
}

Task Publish.SesClient -Depends Build.SesLibrary, Build.SesClient {
    $clientDir = resolve-path $srcDir\sesclient.native\x64\*
    exec {
        compress-archive $clientDir\*.exe, $clientDir\*.dll $publishDir\sesclient-$version-x64.zip
    }
}

## --------------------------------------------------------------------------------
##   Configuration Targets
## --------------------------------------------------------------------------------
## Tasks used to configure the build

Task Request.x64 {
    $script:targetPlatform = "x64"
}

Task Request.x86 {
    $script:targetPlatform = "x86"
}

Task Request.Debug {
    $script:configuration = "Debug"
}

Task Request.Release {
    $script:configuration = "Release"
}

## --------------------------------------------------------------------------------
##   Prerequisite Targets
## --------------------------------------------------------------------------------
## Tasks used to ensure that prerequisites are available when needed. 

Task Requires.Configuration {
    if ($configuration -eq $null) {
        $script:configuration = "Debug"
    }

    Write-Host "Build configuration is $configuration"
}

Task Requires.DotNetExe {
    $script:dotnetExe = (get-command dotnet).Path

    if ($dotnetExe -eq $null) {
        $script:dotnetExe = resolve-path $env:ProgramFiles\dotnet\dotnet.exe -ErrorAction SilentlyContinue
    }
    
    if ($dotnetExe -eq $null) {
        throw "Failed to find dotnet.exe"
    }

    Write-Host "Dotnet executable: $dotnetExe"
}

Task Requires.MsBuild {
    # prefer MSBuild from VS2017 if its there
    $script:msbuildExe = resolve-path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\*\MSBuild\*\Bin\MSBuild.exe" -ErrorAction SilentlyContinue

    if ($msbuildExe -eq $null) {
        $script:msbuildExe = (get-command msbuild).Path
    }

    if ($msbuildExe -eq $null) {
        throw "Failed to find msbuild.exe"
    }

    Write-Host "MSBuild executable: $msbuildExe"
}

Task Requires.OpenCover {

    $script:openCoverExe = resolve-path $env:userprofile\.nuget\packages\OpenCover\*\tools\OpenCover.Console.exe -ErrorAction SilentlyContinue

    if ($openCoverExe -eq $null) {
        throw "Failed to find OpenCover.Console.exe"
    }

    Write-Output "Opencover executable: $openCoverExe"
}

Task Requires.OutDir {
    if (!(test-path $outDir)) {
        mkdir $outDir -ErrorAction SilentlyContinue | Out-Null
    }

    if (!(test-path $outDir)) {
        throw "Output folder does not exist"
    }

    Write-Host "Output folder is $outDir"
}

Task Requires.Platform {

    if ($targetPlatform -eq $null) {
        $script:targetPlatform = "Debug"
    }

    Write-Host "Target platform is $targetPlatform"
}

Task Requires.ReportGenerator {
    
        $script:reportGeneratorExe = resolve-path $env:userprofile\.nuget\packages\reportgenerator\*\tools\ReportGenerator.exe -ErrorAction SilentlyContinue | 
            Sort-Object -Property "Name" | 
            select-object -last 1
    
        if ($reportGeneratorExe -eq $null) {
            throw "Failed to find ReportGenerator.exe"
        }
    
        Write-Output "Report Generator executable: $reportGeneratorExe"
    }

## --------------------------------------------------------------------------------
##   Support Functions
## --------------------------------------------------------------------------------

formatTaskName { 
    param($taskName) 

    $divider = "-" * ((get-host).UI.RawUI.WindowSize.Width - 2)
    return "`r`n$divider`r`n  $taskName`r`n$divider`r`n"
} 

function Write-SubtaskName($subtaskName) {
    $divider = "-" * ($subtaskName.Length + 4)
    Write-Host "`r`n$divider`r`n  $subtaskName`r`n$divider"
}
