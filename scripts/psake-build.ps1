##
##  Psake build file for the Software Entitlement Service SDK
##

properties {
    $baseDir = resolve-path ..\
    $buildDir = "$baseDir\build"
    $srcDir = resolve-path $baseDir\src
    $testsDir = resolve-path $baseDir\tests
}

Task Build.Xplat -Depends Build.SesTest, Unit.Tests

Task Build.Windows -Depends Build.SesLibrary, Build.SesClient

## --------------------------------------------------------------------------------
##   Build Targets
## --------------------------------------------------------------------------------
## Tasks used to perform steps of the actual build

Task Restore.NuGetPackages -Depends Requires.DotNetExe {
    exec {
        & $dotnetExe restore $baseDir\src\sestest
    }
}

Task Build.SesTest -Depends Requires.DotNetExe, Restore.NuGetPackages {
    exec {
        & $dotnetExe build $srcDir\sestest
    }
}

Task Build.SesLibrary -Depends Requires.MsBuild, Requires.Configuration, Requires.Platform {
    exec {
        & $msbuildExe $srcDir\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\ /property:Configuration=$configuration /property:Platform=$targetPlatform
    }
}

Task Build.SesClient -Depends Requires.MsBuild, Requires.Configuration, Requires.Platform {
    exec {
        & $msbuildExe $srcDir\sesclient.native\ /property:Configuration=$configuration /property:Platform=$targetPlatform
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

    if ($dotnetExe -eq $null)
    {
        $script:dotnetExe = resolve-path $env:ProgramFiles\dotnet\dotnet.exe -ErrorAction SilentlyContinue
    }
    
    if ($dotnetExe -eq $null)
    {
        throw "Failed to find dotnet.exe"
    }

    Write-Host "Dotnet executable: $dotnetExe"
}

Task Requires.MsBuild {
    # prefer MSBuild from VS2017 if its there
    $script:msbuildExe = resolve-path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\*\MSBuild\*\Bin\MSBuild.exe" -ErrorAction SilentlyContinue

    if ($msbuildExe -eq $null)
    {
        $script:msbuildExe = (get-command msbuild).Path
    }

    if ($msbuildExe -eq $null)
    {
        throw "Failed to find msbuild.exe"
    }

    Write-Host "MSBuild executable: $msbuildExe"
}

Task Requires.Platform {

    if ($targetPlatform -eq $null) {
        $script:targetPlatform = "Debug"
    }

    Write-Host "Target platform is $targetPlatform"
}

## --------------------------------------------------------------------------------
##   Support Functions
## --------------------------------------------------------------------------------

formatTaskName { 
    param($taskName) 

    $divider = "-" * ((get-host).UI.RawUI.WindowSize.Width - 2)
    return "`r`n$divider`r`n  $taskName`r`n$divider`r`n"
} 

function Write-SubtaskName {
    param($subtaskName)

    $divider = "-" * ($subtaskName.Length + 4)
    Write-Host "`r`n$divider`r`n  $subtaskName`r`n$divider"
}
