# Software Entitlement Service Walk-through

This walk-through will guide you through initial use of the Software Entitlement Service SDK, 

## Prerequisites


## Building the tool

### Checking that it works

From command prompt, run the `sestest` utility to see if it works.

Using the convenience script `sestest.ps1`, run the utility directly from the root directory of the repository:

```
PS> .\sestest
```

If you want/need to bypass the convenience script, use the `dotnet` executable to launch `sestest.dll` from the build directory.

```
C:\...>dotnet .\out\sestest\Debug\netcoreapp1.1\sestest.dll
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

### Cross Platform Test Utility

### Native Client Tool

## Selecting Certificates

## Generating a token

## Starting the test server

## Verifying a token
