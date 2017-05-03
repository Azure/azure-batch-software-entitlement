# Software Entitlement Service Walk-through

This walk-through describes how the *Software Entitlement Service SDK* is used to enable on-premise integration of licensed commercial software with Azure Batch.

## Overview

In production, Azure Batch will generate a software entitlements token for each task run on a compute node. The application software installed on the node (for the purposes of this walk-through, ContosoApp) securely contacts a software entitlement server to verify that the token is authentic. 

![Component integration in the cloud](../img/walk-through-in-cloud.png)

When working on-premise, the Software Entitlement Service SDK substitutes for the components available only in the cloud:

![Component integration for dev test](../img/walk-through-dev-test.png)

## Prerequisites

Before working through this guide, you will need to have built the tooling and have it ready for execution. See the [Guide to building the Software Entitlement Service SDK](build-guide.md) for details of doing this.

## A note on shells

The SDK has been written to be cross-platform, working on Windows, Linux and macOS. For brevity, this walk-through uses **PowerShell** only (usable on both Windows and [Linux](https://azure.microsoft.com/blog/powershell-is-open-sourced-and-is-available-on-linux/)); the commands shown should be trivially convertible to your shell of choice, such as `CMD` and `bash` (including `bash` on Windows 10).

## Selecting Certificates

Before we begin, we need to select one or more digital certificates to use.

The software entitlement service makes use of three digital certificates:

* one to digitally sign the generated entitlement token,
* one to encrypt the generated entitlement token, and
* one to authenticate the software entitlement service.

In production, three different certificates will be used, but for test scenarios you can use the same certificate for all three.

For each required certificate you will need to know its *thumbprint*. Both condensed (e.g. `d4de20d05e66fc53fe1a50882c78db2852cae474`) and expanded (e.g. `d4 de 20 d0 5e 66 fc 53 fe 1a 50 88 2c 78 db 28 52 ca e4 74`) formats are supported.

If you want to reuse an existing certificate or certificates, see below for instructions. 
If you want to generate your own self-signed certificates for use, the blog entry [Creating self signed certificates with makecert.exe for development](https://blog.jayway.com/2014/09/03/creating-self-signed-certificates-with-makecert-exe-for-development/) may be useful.

### Windows

On the Windows platform, one way to find suitable certificates is to use the built in certificate manager.

![Certificate Manager](../img/certificate-manager.png)

At minimum, you must use a certificate that has a private key.

![Certificate with Private Key](../img/certificate-details.png)

### Listing possible certificates

To assist with finding a suitable certificate, the `sestest` utility has a **list-certificates** mode that will list certificates that *may* work (the tool lists certificates with a private key but doesn't check for other characteristics):

``` PowerShell
.\sestest list-certificates
```

The output from this command is tabular, so we recommend using a console window that is as wide as possible.

![Sample output of list-certificates](../img/list-certificates.png)

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

*The `generate` mode of `sestest` is used to generate a software entitlement token. We will use this to generate a token, after which we will manually define the environment variables normally provided by Azure Batch.*

Running `sestest generate` with just the mandatory parameters supplied will generate a minimal token:

``` PowerShell
.\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp
```

```
10:57:15.616 [Information] ---------------------------------------------
10:57:15.616 [Information]   Software Entitlement Service Test Utility
10:57:15.616 [Information] ---------------------------------------------
10:57:15.882 [Information] Token: "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJ2bWlkIjoibWFjaGluZS1pZGVu
... elided ...
L2JhdGNoLmF6dXJlLmNvbS9zb2Z0d2FyZS1lbnRpdGxlbWVudCJ9."
```

Some lines have been removed and the output has been artificially wrapped at 100 columns width. Your token will differ due to different timestamps, machine names and IP addresses. For a full reference of all the available parameters for this mode, see [../src/sestest/readme.md](../src/sestest/readme.md).

We recommend including the option `--log-level debug` to show more information about what is included in the token:

``` PowerShell
.\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp --log-level debug
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
12:27:36.680 [Debug] Not Before: 2017-04-20T12:27
12:27:36.681 [Debug] Not After: 2017-04-27T14:27
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

Set the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` to the generated token value:

``` PowerShell
$env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJ2bWlkIjoiQkVWQU4
...elided... 
mdHdhcmUtZW50aXRsZW1lbnQiLCJhdWQiOiJodHRwczovL2JhdGNoLmF6dXJlLmNvbS9zb2Z0d2FyZS1lbnRpdGxlbWVudCJ9."
```

### Generating signed and encrypted tokens

While working with the software entitlement service SDK, signing and encryption of tokens are both optional. In production, Azure Batch will generate software entitlement tokens that are both signed and encrypted. The digital signature will allow our server to verify a token was generated by us, and the encryption will prevent the signature from being stripped from the token.

We therefore encourage you to do your testing with fully secured tokens.

To sign a token, you will need to specify the thumbprint of a certificate to use for signing. The same thumbprint will need to be specified for both token generation and for the test server (describe below). Encryption works in the same way - the appropriate thumbprint will need to be provided at both ends of the process.

Define `$signingThumbprint` and `$encryptingThumbprint` with the appropriate thumbprint values:

``` PowerShell
$signingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
$encryptingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
```

Add the options `--sign` and `--encrypt` to the command line used above to generate a fully secured token:

``` PowerShell
.\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp --sign $signingThumbprint --encrypt $encryptingThumbprint --log-level debug
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
14:06:43.987 [Debug] Not Before: 2017-04-20T14:06
14:06:43.989 [Debug] Not After: 2017-04-27T14:06
14:06:44.165 [Debug] Raw token: {"alg":"RSA-OAEP","enc":"A256CBC-HS512","kid":"<thumbprint>","typ":
"JWT"}.{"vmid":"machine-identifier","ip":["99.999.999.999","::9","xx99::x99x:99x9:x99:9x9x%9","999.
9.9.9","9999:x999:9999:999:x99x:99x9:x99:9x9x","9999:x999:9999:999:9999:9xx9:9xxx:9xx9"],"app":"con
tosoapp","nbf":1492654003,"exp":1493258803,"iat":1492654003,"iss":"https://batch.azure.com/software
-entitlement","aud":"https://batch.azure.com/software-entitlement"}
14:06:44.172 [Information] Token: "eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZDQkMtSFM1MTIiLCJraWQiOiI2
... elided ...
Ajt9tTffxB6lRlMxeXi25ejR-b4Kul34A3A3w"
```

Set the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` to the generated token value:


``` PowerShell
$env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN = "eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZDQkMtSFM1MTIiLCJ
...elided... Ajt9tTffxB6lRlMxeXi25ejR-b4Kul34A3A3w"
```

Note that an encrypted token is significantly longer, in part due to information about the required key that's included within. One reason that we recommend testing with an encrypted token is to ensure your application can handle the full length of each token.

## Starting the test server

*The **server** mode of `sestest` provides an HTTPS endpoint that acts as a fully functioning software entitlement server that can be used during development and testing.*

The server requires a certificate to secure the HTTPS connection, it will not run without it.

Open a new shell window and the variables `$connectionThumbprint`, `$signingThumbprint` and `$encryptingThumbprint` and  the appropriate thumbprint values. Remember that these values must match the values used earlier when the token was generated.

Then run the server with just the connection thumbprint:

``` PowerShell
$connectionThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
$signingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
$encryptingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
.\sestest server --connection $connectionThumbprint --sign $signingThumbprint --encrypt $encryptingThumbprint
```

The server will start up and wait for connections.

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

(For a full reference of all the available parameters for this mode, see [../src/sestest/readme.md](../src/sestest/readme.md).)

Test the server with a web browser, connecting to the server by entering the URL shown on the console.

![Browser](img/browser.png)

Only an HTTPS connection will work. The server does not listen for HTTP connections. If you specified any kind of locally signed certificate, you will likely need to override security features of your browser to connect.

Leave the server running in this shell window and return to the first. You may want to arrange the two shell windows so you can observe the one running the test server while you work in the original window.

In the original shell window, define an environment variable with the URL of the server:

``` PowerShell
$env:AZ_BATCH_ACCOUNT_URL = "https://localhost:4443"
```

## Verifying a token

*The `sesclient` console application allows you to submit a previously generated token to a software entitlement server and see the result.*

With the environment variables previously defined (`AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` and `AZ_BATCH_ACCOUNT_URL`), plus the certificate variable `$connectionThumbprint`, you should be able to verify the token with this command:

``` PowerShell
.\sesclient --url $env:AZ_BATCH_ACCOUNT_URL --thumbprint $connectionThumbprint --common-name localhost --token $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN --application contosoapp
```

## Troubleshooting

### SSPI Errors

If the connection certificate you selected previously isn't fully trusted, the `sestest server` window will show messages like this:

```
11:48:21.194 [Error] ConnectionFilter.OnConnection
11:48:21.202 [Error] One or more errors occurred. (A call to SSPI failed, see inner exception.) (AggregateException)
11:48:21.203 [Error]     A call to SSPI failed, see inner exception. (AuthenticationException)
11:48:21.234 [Error]         The certificate chain was issued by an authority that is not trusted (Win32Exception)
```

One way to remedy this is to install the certificate as a **Trusted Root Certificate Authority**. Since this is a global configuration change on your machine, please make sure you are comfortable with the consequences before doing this.

### Using a self-signed certificate

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

## Things to try

Once you've successfully validated a token, here are some things to try:

* Try a token that has already expired.
* Try a token that is not yet enabled.
* Try requesting entitlement for an application not listed in the token
* Try requesting entitlement from a different machine on your network
