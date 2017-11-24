# Software Entitlement Service build guide

This *build guide* describes how to build the tools provided as a part of the *Software Entitlement Service SDK*.

## Prerequisites

You will need certain prerequisites installed on your system:

The `sestest` command line utility and associated libraries are written in C# 7 and require version 2.0 or higher of [.NET Core](https://www.microsoft.com/net/core#windowsvs2017) to be installed. The tool was written with Visual Studio 2017; it will compile with just the .NET Core SDK installation. For more information see the [Sestest command line utility](../src/sestest/).

The C++ source for the client library requires [libcurl](https://curl.haxx.se/libcurl/) and [OpenSSL](https://www.openssl.org/) libraries as installed by [vcpkg](https://blogs.msdn.microsoft.com/vcblog/2016/09/19/vcpkg-a-tool-to-acquire-and-build-c-open-source-libraries-on-windows/). The library was also written with Visual Studio 2017; it will compile with any modern C++ compiler. For more information (including details of configuration and use of `vcpkg`) see the [Software entitlement service native client library](../src/Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native)

Build scripts and other tooling are written in PowerShell using the [Psake](https://github.com/psake/psake) make tool. These build scripts work on both Windows and Linux, though initial setup differs.

**Note:** By default, execution of PowerShell scripts is disabled on many Windows systems. If you get an error "*script.ps1* cannot be loaded because running scripts is disabled on this system", you will need to unblock scripts by running `set-executionpolicy remotesigned` from an elevated PowerShell window. 

### Psake On Windows

Preinstallation of Psake is optional; if it isn't already available, the build scripts will attempt to use a local version downloaded via NuGet. (See `scripts/bootstrap.ps1` for details.)

### Psake on Linux

Support for running **psake** on Linux using PowerShell Core hasn't yet (as of September 2017) been released - the current release (v4.6.0, from March 2016) only runs on Windows.

For builds of the software entitlement SDK to run on Linux, you'll need to [download](https://github.com/psake/psake/archive/master.zip) the current `master` branch of psake from GitHub. 

Once downloaded, extract the zip file into the root of the repository as `./lib/psake` so that `bootstrap.ps1` will find the psake PowerShell module as `./lib/psake/psake.psm1`.

This manual installation won't be necessary once a new release of Psake with official Linux support has been made.

## Building `sestest`

*The console application `sestest` is a utility for generating and verifying software entitlement tokens during development and test.*

Open a shell window to the root directory of the repository.

Compile the cross-platform (.NET) tooling with the convenience PowerShell script:

``` PowerShell
.\build-xplat.ps1
```

or compile it manually if you are not using PowerShell:

``` PowerShell
dotnet restore .\src\sestest
dotnet build .\src\sestest
```

### Checking that it works

Run the `sestest` console utility to verify it is ready for use:

``` PowerShell
.\sestest
```

If you are not running on PowerShell, you'll need to use the `dotnet` application to launch the application:

``` bash
dotnet ./out/sestest/Debug/netcoreapp1.1/sestest.dll
```

Either way, you should get output similar to this:

``` 
sestest 1.0.0
Copyright (C) 2017 Microsoft

ERROR(S):
  No verb selected.

  generate             Generate a token with specified parameters

  server               Run as a standalone software entitlement server.

  list-certificates    List all available certificates.

  find-certificate     Show the details of one particular certificate.

  help                 Display more information on a specific command.

  version              Display version information.
```

## Build `sesclient.native`

*The console application `sesclient.native` is a wrapper around the client library that allows a token to be submitted for verification.*

To compile the native code on Windows:

``` PowerShell
.\build-windows -platform x64 -configuration Debug
```

or you can compile it manually:

``` PowerShell
msbuild .\src\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native /property:Configuration=Debug /property:Platform=x64
msbuild .\src\sesclient.native /property:Configuration=Debug /property:Platform=x64
```

The first `msbuild` command shown above builds the library, the second builds a wrapper executable provided for testing purposes.
The commands shown assume that `msbuild` is available on the PATH. If this is not the case, you'll need to provide the full path to `msbuild`. Typically, this is something like `C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe` (the actual path will differ according to the version of Visual Studio or .Net SDK you have installed).

Details of how to build the code will differ if you are using a different C++ compiler or are building on a different platform.

### Checking that it works

Run the `sesclient.native.exe` console utility to verify it is ready for use:

``` PowerShell
.\sesclient.native
```

Again, if you are not running on PowerShell, you'll need run the executable directly from the build output folder:

``` bash
./x64/Debug/sesclient.native.exe
```

You should get output similar to this:

``` 
Contacts the specified Azure Batch software entitlement server to verify the provided token.
Parameters:
    --url <software entitlement server URL>
    --thumbprint <thumbprint of a certificate expected in the server's SSL certificate chain>
    --common-name <common name of the certificate with the specified thumbprint>
    --token <software entitlement token to pass to the server>
    --application <name of the license ID being requested>
```

## Troubleshooting

### Unknown option '/std:c++latest' bootstrapping vcpkg

Compilation errors when running `bootstrat-vcpkg.bat` that include this message:

```
Command line warning D9002: ignoring unknown option '/std:c++latest'
```

May indicate that you have Visual Studio 2015 Update 2 or earlier; **vcpkg** needs at least [Update 3](https://www.visualstudio.com/vs/older-downloads/). 

### Assert: No .NET Framework installation directory found at \Microsoft.NET\Framework64\v4.0.30319\.

This error occurs when attempting to build on Linux using Psake 4.6.0 (the current release as of September 2017).

Release 4.6.0 of Psake doesn't support Linux - but the current `master` branch does.

[Download](https://github.com/psake/psake/archive/master.zip) the current `master` branch of psake and extract the archive into the root of the repository as `./lib/psake` so that `bootstrap.ps1` will find the psake PowerShell module as `./lib/psake/psake.psm1`.
