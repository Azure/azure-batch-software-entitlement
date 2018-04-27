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

# Clean up build artifacts
Task Clean -Depends Clean.SourceFolder, Clean.OutFolder, Clean.PublishFolder

# Build just the cross platform pieces
Task Build.Xplat -Depends Clean, Build.SesTest, Unit.Tests

# Publish distributable zip files for the each tool and platform
Task Publish.Archives -Depends Clean, Request.Release, Publish.SesTest.Win64, Publish.SesTest.Linux64, Publish.SesClient.x64, Publish.SesClient.x86

# Build all the components for use on Windows
Task Build.Windows -Depends Clean, Build.SesClient.x64, Build.SesTest

## --------------------------------------------------------------------------------
##   Preparation Targets
## --------------------------------------------------------------------------------
## Tasks used to prepare for the actual build

Task Restore.NuGetPackages -Depends Requires.DotNetExe {
    # Restore for each C# project to avoid the errors that happen when we try to restore for a C++ project
    foreach ($project in (resolve-path $srcDir\*\*.csproj, $testsDir\*\*.csproj)){
        $projectName = split-path $project -Leaf
        Write-SubtaskName "Restoring packages for $projectName"
        exec {
            & $dotnetExe restore $project --verbosity minimal
        }
    }
}

Task Clean.SourceFolder {
    remove-item $srcDir\*\bin\* -recurse -ErrorAction SilentlyContinue
    remove-item $srcDir\*\obj\* -recurse -ErrorAction SilentlyContinue
    remove-item $srcDir\*\publish\* -recurse -ErrorAction SilentlyContinue

    remove-item $testsDir\*\bin\* -recurse -ErrorAction SilentlyContinue
    remove-item $testsDir\*\obj\* -recurse -ErrorAction SilentlyContinue
    remove-item $testsDir\*\publish\* -recurse -ErrorAction SilentlyContinue
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

    $branch = git name-rev --name-only HEAD
    Write-Output "Branch   $branch"

    $commit = git rev-parse --short head
    Write-Output "Commit   $commit"

    $script:version = "$versionBase.$patchVersion"
    Write-Output "Version: $version"

    if ($branch -eq "master") {
        $script:semanticVersion = $version
    }
    else {
        $semverBranch = $branch -replace "[^A-Za-z0-9-]+", "."
        $script:semanticVersion = "$version-beta.$semverBranch.$commit"
    }

    Write-Output "Semver:  $semanticVersion"
}

Task Build.SesTest -Depends Requires.DotNetExe, Restore.NuGetPackages, Generate.Version {
    $project = resolve-path $srcDir\sestest\sestest.csproj
    Write-Output "Building $project"
    exec {
        & $dotnetExe build $project /property:Version=$semanticVersion /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\sestest.msbuild.log --no-restore
    }
}

Task Build.SesLibrary.x86 -Depends Requires.MsBuild, Requires.Configuration, Generate.Version {
    exec {
        & $msbuildExe $srcDir\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\ /p:Version=$version /property:Configuration=$configuration /property:Platform=x86 /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\seslibrary.x86.msbuild.log
    }
}

Task Build.SesLibrary.x64 -Depends Requires.MsBuild, Requires.Configuration, Generate.Version {
    exec {
        & $msbuildExe $srcDir\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\ /p:Version=$version /property:Configuration=$configuration /property:Platform=x64 /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\seslibrary.msbuild.log
    }
}

Task Build.SesClient.x86 -Depends Requires.MsBuild, Requires.Configuration, Build.SesLibrary.x86, Generate.Version {
    exec {
        & $msbuildExe $srcDir\sesclient.native\ /property:Configuration=$configuration /p:Version=$version /property:Platform=x86 /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\sesclient.msbuild.log
    }
}

Task Build.SesClient.x64 -Depends Requires.MsBuild, Requires.Configuration, Build.SesLibrary.x64, Generate.Version {
    exec {
        & $msbuildExe $srcDir\sesclient.native\ /property:Configuration=$configuration /p:Version=$version /property:Platform=x64 /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\sesclient.msbuild.log
    }
}

Task Build.UnitTests -Depends Requires.DotNetExe {
    foreach ($project in (resolve-path $testsDir\*\*.csproj)){
        $projectName = split-path $project -Leaf
        Write-SubtaskName "Building $projectName"
        exec {
            & $dotnetExe build $project /property:Version=$semanticVersion /verbosity:minimal /fileLogger /flp:verbosity=detailed`;logfile=$outDir\$projectName.msbuild.log --no-restore
        }
    }
}

Task Unit.Tests -Depends Requires.DotNetExe, Build.UnitTests {
    foreach ($project in (resolve-path $testsDir\*\*.csproj)){
        $projectName = split-path $project -Leaf
        Write-SubtaskName "Running unit tests from $projectName"
        exec {
            & $dotnetExe test $project --no-build
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

Task Publish.SesTest.Win64 -Depends Requires.DotNetExe, Restore.NuGetPackages, Generate.Version {
    exec {
        & $dotnetExe publish $srcDir\sestest\sestest.csproj --self-contained --output $publishDir\sestest\win10-x64 --runtime win10-x64 /p:Version=$semanticVersion
    }

    $archive = "$publishDir\sestest-$semanticVersion-win10-x64.zip"
    exec {
        compress-archive $publishDir\sestest\win10-x64\* $archive
    }

    Write-Output "Created archive $archive"
}

Task Publish.SesTest.Linux64 -Depends Requires.DotNetExe, Restore.NuGetPackages, Generate.Version {
    exec {
        & $dotnetExe publish $srcDir\sestest\sestest.csproj --self-contained --output $publishDir\sestest\linux-x64 --runtime linux-x64 /p:Version=$semanticVersion
    }

    $archive = "$publishDir\sestest-$semanticVersion-linux-x64.zip"
    exec {
        compress-archive $publishDir\sestest\linux-x64\* $archive
    }

    Write-Output "Created archive $archive"
}

Task Publish.SesClient.x86 -Depends Build.SesLibrary.x86, Build.SesClient.x86 {
    $clientDir = resolve-path $srcDir\sesclient.native\$configuration
    $archive = "$publishDir\sesclient-$semanticVersion-x86.zip"
    
    exec {
        compress-archive $clientDir\*.exe, $clientDir\*.dll $archive
    }

    Write-Output "Created archive $archive"
}

Task Publish.SesClient.x64 -Depends Build.SesLibrary.x64, Build.SesClient.x64 {
    $clientDir = resolve-path $srcDir\sesclient.native\x64\$configuration
    $archive = "$publishDir\sesclient-$semanticVersion-x64.zip"
    
    exec {
        compress-archive $clientDir\*.exe, $clientDir\*.dll $archive
    }

    Write-Output "Created archive $archive"
}

## --------------------------------------------------------------------------------
##   Configuration Targets
## --------------------------------------------------------------------------------
## Tasks used to configure the build

Task Request.Debug {
    Write-Output "Debug build"
    $script:configuration = "Debug"
}

Task Request.Release {
    Write-Output "Release build"
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

    Write-Output "Build configuration is $configuration"
}

Task Requires.DotNetExe {
    $script:dotnetExe = (get-command dotnet).Path

    if ($dotnetExe -eq $null) {
        $script:dotnetExe = resolve-path $env:ProgramFiles\dotnet\dotnet.exe -ErrorAction SilentlyContinue
    }
    
    if ($dotnetExe -eq $null) {
        throw "Failed to find dotnet.exe"
    }

    Write-Output "Dotnet executable: $dotnetExe"
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

    Write-Output "MSBuild executable: $msbuildExe"
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

    Write-Output "Output folder is $outDir"
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
    Write-Output "`r`n$divider`r`n  $subtaskName`r`n$divider"
}
