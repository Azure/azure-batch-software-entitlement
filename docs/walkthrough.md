# Software Entitlement Service Walk-through

This walk-through will guide you through initial use of the Software Entitlement Service SDK, building the tooling from source code, generating and then verifying a software entitlement token.

## Table of Contents

* [Prerequisites](#prerequisites)
* [Building the tools](#building-the-tools)
* [Selecting Certificates](#selecting-certificates)
* [Generating a token](#generating-a-token)
* [Starting the test server](#starting-the-test-server)
* [Verifying a token](#verifying-a-token)

## Prerequisites

To build and use the Software Entitlement Services Test tool (`sestest`) you will need certain prerequisites installed on your system:

The `sestest` command line application and associated assemblies are written in C#7 and require version 1.1 or higher of [.NET Core](https://www.microsoft.com/net/core#windowsvs2017) to be pre-installed. The tool was written with Visual Studio 2017; it should compile with just the .NET Core installation. For more information see the [Sestest command line utility](../src/sestest/).

The C++ source for the client library requires [libcurl](https://curl.haxx.se/libcurl/) and [OpenSSL](https://www.openssl.org/) libraries as packaged by [vcpkg](https://blogs.msdn.microsoft.com/vcblog/2016/09/19/vcpkg-a-tool-to-acquire-and-build-c-open-source-libraries-on-windows/). The library was also written with Visual Studio 2017; it should compile with any modern C++ compiler. For more information (including details of configuration and use of `vcpkg`) see the [Software entitlement service native client library](../src/Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native)

## Building the tools

To compile the cross platform (.NET) based tooling, open a console window to the root directory of the repo and run one of the following commands:

| Console    | Command                                                           |
| ---------- | ----------------------------------------------------------------- |
| PowerShell | `.\build-xplat.ps1`                                               |
| Cmd        | `dotnet restore .\src\sestest` <br/> `dotnet build .\src\sestest` |
| bash       | `dotnet restore ./src/sestest` <br/> `dotnet build ./src/sestest` |

* The choice of command is specific to the console being used, not the host platform, and has the 
  usual predictable differences between consoles. The PowerShell convenience script should work 
  the same on Linux as it does on Windows; the bash commands should work the same on Windows 10 as 
  they do on Linux.

To compile the native code on Windows, run one of the following commands:

| Console    | Command                                                                                                                                                                                                                  |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| PowerShell | `.\build-windows -platform x64 -configuration Debug`                                                                                                                                                                     |
| Cmd        | `msbuild .\src\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native /property:Configuration=Debug /property:Platform=x64` <br/> `msbuild .\src\sesclient.native /property:Configuration=Debug /property:Platform=x64` |

* These commands presume an installation of Visual Studio 2017 on the machine that includes the C++ compiler toolset
* The PowerShell convenience script uses **msbuild** directly.
* The command given for **cmd** assumes that **msbuild** is availble on the PATH (as it will be if you open a *Developer Command Prompt for VS 2017* window)
* The first `msbuild` command shown above builds the library, the second builds a wrapper executable provided for testing purposes.
* Details of the required will differ if you are using a different C++ compiler.

### Troubleshooting the builds

TBC

### Checking that it works

If compilation works without any issues, you should now have the executables you need for testing.

To verify that the `sestest` console application is ready for use, run one of the following commands from your console:

| Console    | Command                                                |
| ---------- | ------------------------------------------------------ |
| PowerShell | `.\sestest`                                            |
| Cmd        | `dotnet .\out\sestest\Debug\netcoreapp1.1\sestest.dll` |
| bash       | `dotnet ./out/sestest/Debug/netcoreapp1.1/sestest.dll` |

* Again, there is a PowerShell convenience script for use.

You should get output similar to this:

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

To verify the `sesclient.native.exe` console application is ready for use, run one of the following commands from your console:

| Console    | Command                            |
| ---------- | ---------------------------------- |
| PowerShell | `.\x64\Debug\sesclient.native.exe` |
| Cmd        | `.\x64\Debug\sesclient.native.exe` |
| bash       | `./x64/Debug/sesclient.native`     |

## Selecting Certificates

## Generating a token

## Starting the test server

## Verifying a token
