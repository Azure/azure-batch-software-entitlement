# Software Entitlement Service Walk-through

This walk-through will guide you through use of the Software Entitlement Service SDK, building the tooling from source code, generating and verifying a software entitlement token.

## Table of Contents

* [Prerequisites](#prerequisites)
* [Building the tools](#building-the-tools)
* [Selecting Certificates](#selecting-certificates)
* [Generating a token](#generating-a-token)
* [Starting the test server](#starting-the-test-server)
* [Verifying a token](#verifying-a-token)
* [Bringing it all together](#bringing-it-all-together)

## A note on shells

The SDK has been written to be cross-platform, working on Windows, Linux and macOS. For brevity, this walk-through uses **PowerShell** only (usable on both Windows and [Linux](https://azure.microsoft.com/blog/powershell-is-open-sourced-and-is-available-on-linux/)); the commands shown should be trivially convertible to your shell of choice, such as `CMD` and `bash` (including `bash` on Windows 10).

## Prerequisites

To build and use the Software Entitlement Services test tool (`sestest`) you will need certain prerequisites installed on your system:

The `sestest` command line utility and associated libraries are written in C#7 and require version 1.1 or higher of [.NET Core](https://www.microsoft.com/net/core#windowsvs2017) to be installed. The tool was written with Visual Studio 2017; it will compile with just the .NET Core SDK installation. For more information see the [Sestest command line utility](../src/sestest/).

The C++ source for the client library requires [libcurl](https://curl.haxx.se/libcurl/) and [OpenSSL](https://www.openssl.org/) libraries as installed by [vcpkg](https://blogs.msdn.microsoft.com/vcblog/2016/09/19/vcpkg-a-tool-to-acquire-and-build-c-open-source-libraries-on-windows/). The library was also written with Visual Studio 2017; it will compile with any modern C++ compiler. For more information (including details of configuration and use of `vcpkg`) see the [Software entitlement service native client library](../src/Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native)

## Building the tools

Open a shell window to the root directory of the repository.

Compile the cross-platform (.NET) tooling with the convenience PowerShell script:

``` PowerShell
.\build-xplat.ps1
```

or compile it manually:

``` PowerShell
dotnet restore .\src\sestest
dotnet build .\src\sestest
```

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
The commands shown assume that `msbuild` is available on the PATH. If this is not the case, you'll need to provide the full path to `msbuild`&ntypically, this is something like `C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe` (the actual path will differ according to the version of Visual Studio or SDK you have installed).

Details of how to build the code will differ if you are using a different C++ compiler or are building on a different platform.

### Checking that it works

If compilation works without any issues, you should now have the executables you need for testing.

Run the `sestest` console utility to verify it is ready for use:

``` PowerShell
.\sestest
```

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

Run the `sesclient.native.exe` console utility to verify it is ready for use:

``` PowerShell
.\sesclient.native
```

You should get output similar to this:

``` 
Contacts the specified azure batch software entitlement server to verify the provided token.
Parameters:
    --url <software entitlement server URL>
    --thumbprint <thumbprint of a certificate expected in the server's SSL certificate chain>
    --common-name <common name of the certificate with the specified thumbprint>
    --token <software entitlement token to pass to the server>
    --application <name of the license ID being requested>
```

## Selecting Certificates

The software entitlement service makes use of three digital certificates:

* one to digitally sign the generated entitlement token,
* one to encrypt the generated entitlement token, and
* one to authenticate the software entitlement service.

In production, three different certificates will be used, but for test scenarios you can use the same certificate for all three.

For each required certificate you will need to know its *thumbprint*. Both condensed (e.g. `d4de20d05e66fc53fe1a50882c78db2852cae474`) and expanded (e.g. `d4 de 20 d0 5e 66 fc 53 fe 1a 50 88 2c 78 db 28 52 ca e4 74`) formats are supported.

### Windows

On the Windows platform, one way to find suitable certificates is to use the built in certificate manager.

![Certificate Manager](img/certificate-manager.png)

At minimum, you must use a certificate that has a private key.

![Certificate with Private Key](img/certificate-details.png)

### Listing possible certificates

To assist with finding a suitable certificate, the `sestest` utility has a **list-certificates** mode that will list certificates that *may* work (the tool lists certificates with a private key but doesn't check for other characteristics):

``` PowerShell
.\sestest list-certificates
```

The output from this command is tabular, so we recommend using a console window that is as wide as possible.

![Sample output of list-certificates](img/list-certificates.png)

(Yes, this output is obfuscated.)

### Checking a thumbprint

Once you've selected a thumbprint for use, you can verify it using `sestest` (Substitute your own thumbprint for `XXX`):

``` PowerShell
.\sestest find-certificate --thumbprint XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

For a thumbprint containing whitespace (as it will if copied from the Windows certificate properties dialog), wrap the thumbprint in quotes:

``` PowerShell
.\sestest find-certificate --thumbprint "XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX"
```

If `sestest` finds the certificate, some information will be shown:

``` 
10:26:13.119 [Information] ---------------------------------------------
10:26:13.119 [Information]   Software Entitlement Service Test Utility
10:26:13.119 [Information] ---------------------------------------------
10:26:13.168 [Information] [Subject]
10:26:13.170 [Information]   CN=localhost
10:26:13.171 [Information]
10:26:13.171 [Information] [Issuer]
10:26:13.172 [Information]   CN=localhost
10:26:13.174 [Information]
10:26:13.175 [Information] [Serial Number]
10:26:13.176 [Information]   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
10:26:13.177 [Information]
10:26:13.180 [Information] [Not Before]
10:26:13.182 [Information]   7/12/2016 10:50:46 AM
10:26:13.182 [Information]
10:26:13.184 [Information] [Not After]
10:26:13.185 [Information]   7/12/2021 12:00:00 PM
10:26:13.186 [Information]
10:26:13.187 [Information] [Thumbprint]
10:26:13.188 [Information]   XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

If `sestest` is unable to find the certificate, you will get an error:

``` 
10:34:59.211 [Information] ---------------------------------------------
10:34:59.211 [Information]   Software Entitlement Service Test Utility
10:34:59.211 [Information] ---------------------------------------------
10:34:59.305 [Error] Did not find cert certificate XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

## Generating a token

The `generate` mode of `sestest` is used to generate a token. The command has the following parameters:

| Parameter        | Required  | Definition                                                                                                                                                                           |
| ---------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| --application-id | Mandatory | Unique identifier(s) for the application(s) to include in the entitlement (comma separated).                                                                                         |
| --vmid           | Mandatory | Unique identifier for the Azure virtual machine. If you are testing outside of Azure, we suggest you use the name of the machine (e.g. `%COMPUTERNAME%`).                            |
| --not-before     | Optional  | The moment at which the token becomes active and the application is entitled to execute <br/> Format: 'yyyy-mm-ddThh-mm'; 24 hour clock; local time; defaults to now.                |
| --not-after      | Optional  | The moment at which the token expires and the application is no longer entitled to execute <br/> Format: 'yyyy-mm-ddThh-mm'; 24 hour clock; local time; defaults to 7 days from now. |
| --address        | Optional  | The IP addresses of the machine entitled to execute the application(s). <br/> Defaults to all the IP addresses of the current machine.                                               |
| --sign           | Optional  | Certificate thumbprint of the certificate to use for signing the token                                                                                                               |
| --encrypt        | Optional  | Certificate thumbprint of the certificate to use for encryption of the token.                                                                                                        |
| --token-file     | Optional  | The name of a file into which the token will be written <br/> If not specified, the token will be shown in the log.                                                                  |
| --log-level      | Optional  | Specify the level of logging output. <br/> One of *error*, *warning*, *information* or *debug*; defaults to *information*.                                                           |
| --log-file       | Optional  | Specify a file into which log messages should be written. <br/> Logging is shown on the console by default.                                                                          |

You can see this documentation for yourself by running `sestest generate --help` in your console.

Running `sestest generate` with no parameters will tell you about the mandatory parameters:

``` PowerShell
.\sestest generate
```

```
10:53:59.102 [Information] ---------------------------------------------
10:53:59.102 [Information]   Software Entitlement Service Test Utility
10:53:59.102 [Information] ---------------------------------------------
10:53:59.164 [Error] No applications specified.
10:53:59.164 [Error] No virtual machine identifier specified.
```

The output tells you that you need to supply both an application and a virtual machine identifier.

Running `sestest generate` with just the mandatory parameters supplied will generate a minimal token:

``` PowerShell
.\sestest generate --vmid machine-identifier --application-id contosoapp
```

```
10:57:15.616 [Information] ---------------------------------------------
10:57:15.616 [Information]   Software Entitlement Service Test Utility
10:57:15.616 [Information] ---------------------------------------------
10:57:15.882 [Information] Token: "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJ2bWlkIjoibWFjaGluZS1pZGVu
... elided ...
L2JhdGNoLmF6dXJlLmNvbS9zb2Z0d2FyZS1lbnRpdGxlbWVudCJ9."
```
(This has been artificially wrapped at 100 columns width.)

Include the option `--log-level debug` to get more information about what is included in the token:

``` PowerShell
.\sestest generate --vmid machine-identifier --application-id contosoapp --log-level debug
```

```
12:27:36.577 [Information] ---------------------------------------------
12:27:36.577 [Information]   Software Entitlement Service Test Utility
12:27:36.577 [Information] ---------------------------------------------
12:27:36.656 [Debug] Virtual machine Id: machine-identifier
12:27:36.668 [Debug] IP Address: 99.999.999.999
12:27:36.669 [Debug] IP Address: xx99::x99x:99x9:x99:9x9x%9
12:27:36.670 [Debug] IP Address: ::9
12:27:36.671 [Debug] IP Address: 999.9.9.9
12:27:36.673 [Debug] IP Address: 9999:x999:9999:999:x99x:99x9:x99:9x9x
12:27:36.674 [Debug] IP Address: 9999:x999:9999:999:9999:9xx9:9xxx:9xx9
12:27:36.679 [Debug] Application Id: contosoapp
12:27:36.680 [Debug] Not Before: 4/20/2017 12:27:36 PM +12:00
12:27:36.681 [Debug] Not After: 4/27/2017 12:27:36 PM +12:00
12:27:36.812 [Debug] Raw token: {"alg":"none","typ":"JWT"}.{"vmid":"machine-identifier","ip":["99.9
99.999.999","xx99::x99x:99x9:x99:9x9x%9","::9","999.9.9.9","9999:x999:9999:999:x99x:99x9:x99:9x9x",
"9999:x999:9999:999:9999:9xx9:9xxx:9xx9"],"app":"contosoapp","nbf":1492648056,"exp":1493252856,"iat
":1492648056,"iss":"https://batch.azure.com/software-entitlement","aud":"https://batch.azure.com/so
ftware-entitlement"}
12:27:36.818 [Information] Token: "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJ2bWlkIjoibWFjaGluZS1pZGVu
... elided ...
L2JhdGNoLmF6dXJlLmNvbS9zb2Z0d2FyZS1lbnRpdGxlbWVudCJ9."
```

Note the `[Debug]` log lines that show the actual values that have been used, including the default values selected for parameters we haven't supplied ourselves, such as `--not-before`, `--not-after` and `--address`. (Again, the above output has been wrapped to 100 columns and partially obfuscated.)

To digitally sign the token, define `$signingThumbprint` with the thumbprint of an appropriate certificate and include `--sign $signingThumbprint` on the command line.
Similarly, to encrypt the token define `$encryptingThumbprint` with a certificate thumbprint and include `--encrypt $encryptingThumbprint` on the command line. 
(Defining variables to hold the thumbprints makes it easier to reuse the values later on.)

A full command line that both signs and encrypts a token would look like this:

``` PowerShell
.\sestest generate --vmid machine-identifier --application-id contosoapp --sign $signingThumbprint --encrypt $encryptingThumbprint --log-level debug
```

```
14:06:43.861 [Information] ---------------------------------------------
14:06:43.861 [Information]   Software Entitlement Service Test Utility
14:06:43.861 [Information] ---------------------------------------------
14:06:43.966 [Debug] Virtual machine Id: machine-identifier
14:06:43.977 [Debug] IP Address: 99.999.999.999
14:06:43.977 [Debug] IP Address: ::9
14:06:43.980 [Debug] IP Address: xx99::x99x:99x9:x99:9x9x%9
14:06:43.980 [Debug] IP Address: 999.9.9.9
14:06:43.982 [Debug] IP Address: 9999:x999:9999:999:x99x:99x9:x99:9x9x
14:06:43.982 [Debug] IP Address: 9999:x999:9999:999:9999:9xx9:9xxx:9xx9
14:06:43.985 [Debug] Application Id: contosoapp
14:06:43.987 [Debug] Not Before: 4/20/2017 2:06:43 PM +12:00
14:06:43.989 [Debug] Not After: 4/27/2017 2:06:43 PM +12:00
14:06:44.165 [Debug] Raw token: {"alg":"RSA-OAEP","enc":"A256CBC-HS512","kid":"<thumbprint>","typ":
"JWT"}.{"vmid":"machine-identifier","ip":["99.999.999.999","::9","xx99::x99x:99x9:x99:9x9x%9","999.
9.9.9","9999:x999:9999:999:x99x:99x9:x99:9x9x","9999:x999:9999:999:9999:9xx9:9xxx:9xx9"],"app":"con
tosoapp","nbf":1492654003,"exp":1493258803,"iat":1492654003,"iss":"https://batch.azure.com/software
-entitlement","aud":"https://batch.azure.com/software-entitlement"}
14:06:44.172 [Information] Token: "eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZDQkMtSFM1MTIiLCJraWQiOiI2
... elided ...
Ajt9tTffxB6lRlMxeXi25ejR-b4Kul34A3A3w"
```

An encrypted token is significantly longer, in part due to information about the required key that's included within.
In production, all tokens will be both signed and encrypted and we therefore encourage you to do all your testing with signed and encrypted tokens as well.

## Starting the test server

The **server** mode of `sestest` provides an HTTPS endpoint that acts as a fully functioning software entitlement server that can be used during development and testing. The command has the following parameters:

| Parameter    | Required  | Definition                                                                                                                              |
| ------------ | --------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| --connection | Mandatory | Thumbprint of the certificate to use with HTTPS.                                                                                        |
| --sign       | Optional  | Thumbprint of the certificate used to sign tokens. <br/> If specified, only tokens signed with this certificate will be approved.       |
| --encrypt    | Optional  | Thumbprint of the certificate used to encrypt tokens. <br/> If specified, only tokens encrypted with this certificate will be approved. |
| --url        | Optional  | The URL at which the server should process requests <br/> Defaults to `https://localhost:4443`; must start with `https:`.               |
| --log-level  | Optional  | Specify the level of logging output.<br/>One of *error*, *warning*, *information* or *debug*; defaults to *information*.                |
| --log-file   | Optional  | Specify a file into which log messages should be written.                                                                               |

You can see this documentation for yourself by running `sestest server --help` in your shell.

Run the server with minimum parameters (just a connection certificate thumbprint):

``` PowerShell
.\sestest server --connection <thumbprint>
```

The server will start up and wait for connections

```
17:20:02.676 [Information] ---------------------------------------------
17:20:02.695 [Information]   Software Entitlement Service Test Utility
17:20:02.696 [Information] ---------------------------------------------
17:20:02.977 [Debug] Hosting starting
17:20:03.043 [Debug] Hosting started
Hosting environment: Production
Content root path: ... elided ...
Now listening on: https://localhost:4443
Application started. Press Ctrl+C to shut down.
```

Using a web browser, connect to the server by entering the URL shown on the console.

![Browser](img/browser.png)

* Only an HTTPS connection will work. The server does not listen for HTTP connections.
* If you specified any kind of locally signed certificate, you will likely need to override security features of your browser to connect.

Use Ctrl+C to shut down the server when no longer needed.

```
Application is shutting down...
17:20:09.008 [Debug] Hosting shutdown
```

***TODO: Checking the server is running***

## Verifying a token

The `sesclient` console application allows you to submit a previously generated token to a software entitlement server and see the result. The command has the following parameters:

| Parameter     | Required  | Definition                                                                                      |
| ------------- | --------- | ----------------------------------------------------------------------------------------------- |
| --url         | Mandatory | The url of the software entitlement server to contact for token verification.                   |
| --thumbprint  | Mandatory | Thumbprint of a certificate expected in the server's SSL certificate chain                      |
| --common-name | Mandatory | Common name of the certificate with the specified thumbprint                                    |
| --token       | Mandatory | Software entitlement token to pass to the server <br/> Specify `-` to read the token from stdin |
| --application | Mandatory | Name of the license ID being requested                                                          |

Assuming a token previously generated into `token.txt`:

``` PowerShell
$token = get-content token.txt
.\sesclient --url https://localhost:4443 --thumbprint XXXX --common-name localhost --token $token --application contosoapp
```

Alternatively, in `bash` or `CMD` you can use a redirect to feed the token in by specifying `-` for `--token`:

``` sh
sesclient --url https://localhost:4443 --thumbprint XXXX --common-name localhost --token - --application contosoapp < token.txt
```

## Bringing it all together

Now that we've seen all of the individual components of the SDK, let's pull them all together into a full workflow.

### Select your certificates

Select the certificate or certificates you want to use &ndash; one to secure the connection to the server, one to sign each token and one to encrypt each token. Define three variables, one for each certificate (this makes it easier to reference the thumbprints later on):

``` PowerShell
$connectionThumbprint = "9X9X9X99X9X99X99X999XX9999XX99XX9999XX9X"
$signingThumbprint    = "99999999XX999X999X999X999X9999X99XX99X99"
$encryptingThumbprint = "99X9999XXX9XX9999XX99X9X9999X9X9999X999X"
```

### Generate a token

Generate an encrypted and signed token for the application `contosoapp`:

``` PowerShell
.\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp --sign $signingThumbprint --encrypt $encryptingThumbprint --token-file token.txt
```

This uses the name of your current computer as the "virtual machine identifier" to embed in the token.

```
11:30:29.763 [Information] ---------------------------------------------
11:30:29.783 [Information]   Software Entitlement Service Test Utility
11:30:29.784 [Information] ---------------------------------------------
11:30:30.091 [Information] Token file: "...elided...\token.txt"
```
You may want to inspect the token file using a text editor.

### Start the software entitlement server

Open a second shell window and define the same certificate variables as above. Start a test server:

``` PowerShell
.\sestest server --connection $connectionThumbprint --sign $signingThumbprint --encrypt $encryptingThumbprint
```

The server will start up:

```
11:44:48.705 [Information] ---------------------------------------------
11:44:48.724 [Information]   Software Entitlement Service Test Utility
11:44:48.725 [Information] ---------------------------------------------
Hosting environment: Production
Content root path: ... elided ...
Now listening on: https://localhost:4443
Application started. Press Ctrl+C to shut down.
```

The "Now listening on:" line gives you the URL needed for the next step.

### Checking the token

Back in your original shell window, use `sesclient` to verify the token:

``` PowerShell
$token = get-content token.txt
.\sesclient --url https://localhost:4443 --thumbprint $connectionThumbprint --common-name localhost --token $token --application contosoapp
```

### Troubleshooting

#### SSPI Errors

If the connection certificate you selected previously isn't fully trusted, the `sestest server` window will show messages like this:

```
11:48:21.194 [Error] ConnectionFilter.OnConnection
11:48:21.202 [Error] One or more errors occurred. (A call to SSPI failed, see inner exception.) (AggregateException)
11:48:21.203 [Error]     A call to SSPI failed, see inner exception. (AuthenticationException)
11:48:21.234 [Error]         The certificate chain was issued by an authority that is not trusted (Win32Exception)
```

One way to remedy this is to install the certificate as a **Trusted Root Certificate Authority**. Since this is a global configuration change on your machine, please make sure you are comfortable with the consequences before doing this.

#### Using a self-signed certificate

If using a self-signed certificate, secure connection validation code in the native client library will prevent you from connecting. The error message reads:

```
libcurl_error 60: SSL certificate problem: unable to get local issuer certificate
```

To turn off these checks, modify the source code in `SoftwareEntitlementClient.cpp`, around line #335:

``` cplusplus
// During testing, if the certificate chain leaves something to be
// desired, disable the following two options by setting them to 0.
//
ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSL_VERIFYHOST, /* 2 */ 0));
ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSL_VERIFYPEER, /* 1 */ 0));
```

**NOTE**: Don't leave these checks disabled when you do the release build of your package. Failing to restore these checks will make it much easier for a man-in-the-middle attack.

### Things to try

Once you've successfully validated a token, here are some things to try:

* Try a token that has already expired.
* Try a token that is not yet enabled.
* Try requesting entitlement for an application not listed in the token
* Try requesting entitlement from a different machine on your network
