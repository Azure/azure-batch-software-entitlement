# Software Entitlement Service Walk-through

Many independent software vendors (ISVs) require users to obtain a license from a server in order to run their software. The traditional license server model is difficult to implement in the cloud. Azure Batch provides a way for ISVs who partner with Microsoft to meter usage, and to enforce that users may only use their software within the Azure Batch environment where that usage can be metered, without the need for a license server.

This walk-through describes how the *Software Entitlement Service SDK* is used to enable integration of licensed commercial software with Azure Batch.

## Overview

In production, Azure Batch will generate a software entitlements token for each task run on a compute node. The application software installed on the node (for the purposes of this walk-through, ContosoApp) securely contacts a software entitlement server to verify that the token is authentic. 

![Component integration in the cloud](../img/walk-through-in-cloud.png)

During development, it is important to be able to test locally. The Software Entitlement Service SDK enabless this by providing substitutes for the components normally available only in the cloud:

![Component integration for dev test](../img/walk-through-dev-test.png)

## Prerequisites

Before working through this guide, you will need to have built the tooling and have it ready for execution. See the [Guide to building the Software Entitlement Service SDK](build-guide.md) for details of doing this.

## A note on shells

The SDK has been written to be cross-platform, working on Windows, Linux and macOS. For brevity, this walk-through uses **PowerShell** only (usable on both Windows and [Linux](https://azure.microsoft.com/blog/powershell-is-open-sourced-and-is-available-on-linux/)); the commands shown should be trivially convertible to your shell of choice, such as `CMD` and `bash` (including `bash` on Windows 10).

## Selecting Certificates

In production, the software entitlement service generates tokens that are both digitally signed and encrypted in order to prevent misuse. The service also requires connections to be made via HTTPS. To simulate this in a local dev/test environment, we need to select one or more digital certificates that are available on the local machine.

The software entitlement service makes use of three digital certificates:

* one to digitally sign the generated entitlement token,
* one to encrypt the generated entitlement token, and
* one to authenticate the software entitlement service.

In production, three different certificates will be used, but for test scenarios you can use the same certificate for all three.

For each required certificate you will need to know its *thumbprint*. Both condensed (e.g. `d4de20d05e66fc53fe1a50882c78db2852cae474`) and expanded (e.g. `d4 de 20 d0 5e 66 fc 53 fe 1a 50 88 2c 78 db 28 52 ca e4 74`) formats are supported.

If you already have suitable certificates on your machine (e.g. if you use HTTPS locally for development and testing), you can use those. See [*Finding Certificates*](finding-certificates.md) for information on finding a certificate on your machine.

If you don't already have suitable certificates (or if you're not sure), creating your own certificates is straightforward. The blog entry [Creating self signed certificates with makecert.exe for development](https://blog.jayway.com/2014/09/03/creating-self-signed-certificates-with-makecert-exe-for-development/) is one useful guide for this.

## Generating a token

In production, Azure Batch will generate a software entitlement token and make it available in the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN`. For local development and testing, we can use `generate` mode of `sestest` to generate a token and then manually define the required variable.

Run the following command to generate a minimal token and store it in `token.txt`:

``` PowerShell
PS> .\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp --token-file token.txt
10:18:27.531 [Information] ---------------------------------------------
10:18:27.548 [Information]   Software Entitlement Service Test Utility
10:18:27.548 [Information] ---------------------------------------------
10:18:27.745 [Information] Token file: "token.txt"
```

The `token.txt` file will contain the software entitlement token, stored as a single line of encoded text. 

To see more details about what's included in the token, optionally include the option `--log-level debug`. For a full reference of all the available parameters for this mode, see [../src/sestest/readme.md](../src/sestest/readme.md).

Set the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` to the content of the `token.txt` file:

``` PowerShell
PS> $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN = (get-content token.txt)
```

Or, if you're using CMD:

``` cmd
C:> set /p AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN= < token.txt
```

The `/p` option is required to read the value for the variable from stdin.

### Generating signed and encrypted tokens

In production, Azure Batch generates software entitlement tokens that are both signed and encrypted. The digital signature  allows the production software entitlement server to verify tokens were generated by Azure Batch, and the encryption prevents the signature from being stripped from the token and replaced.

When working in a local development/test environment, signing and encryption of tokens are both optional. We do recommend that you do at least some testing with fully secured tokens as they are significantly longer.

To sign a token, you will need to specify the thumbprint of a certificate to use for signing. Remember which certificate you use for signing as you'll need to provide the same thumbprint to  `sestest server` for verifying the signature (described [below](#starting-the-test-server)).

Encryption works in the same way - the same thumbprint needs to be provided both for token generation and for token verification.

You may use the same certificate for both signing and encryption if you prefer.

Define `$signingThumbprint` and `$encryptingThumbprint` with the appropriate thumbprint values to make it easier to reference the thumbprints:

``` PowerShell
PS> $signingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
PS> $encryptingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
```

Defining these variables makes it easier to type the commands and reducing the opportunity for error. (Both `PowerShell` and `bash` do auto completion of variable names.)

Add the options `--sign` and `--encrypt` to the command line used above to generate a fully secured token:

``` PowerShell
PS> .\sestest generate --vmid $env:COMPUTERNAME --application-id contosoapp --sign $signingThumbprint --encrypt $encryptingThumbprint --token-file token.txt
14:06:43.861 [Information] ---------------------------------------------
14:06:43.861 [Information]   Software Entitlement Service Test Utility
14:06:43.861 [Information] ---------------------------------------------
14:06:44.172 [Information] Token file: "token.txt"
```

Open `token.txt` in your favorite text editor; you'll see that the secured token is substantially longer than an unsecured one.

As we did above, set the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` to the generated token value:

``` PowerShell
PS> $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN = (get-content token.txt)
```

Testing with an encrypted token is useful because these are significantly longer, in part due to information about the required key that's included within. Note that your application doesn't need to do anything different with encrypted vs unencrypted tokens.

## Starting the test server

In production, Azure Batch provides a software entitlement server that will verify a software entitlement token is valid. It also validates that the compute node requesting verification of the token is the virtual machine to which the token was issued.

Once your application has read the token from the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN`, it will call the software entitlement service to verify the token grants permission to use your application.

The production software entitlement server only accepts connections from Azure Batch compute nodes. To enable your local development/test environment, the `server` mode of `sestest` provides an HTTPS endpoint that acts as a fully functioning server.

Open a new shell window and set the variable `$connectionThumbprint` to specify the certificate that should be used for HTTPS.

``` PowerShell
PS> $connectionThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
```

If your token was encrypted and/or secured, set the variables `$signingThumbprint` and `$encryptingThumbprint` to the same thumbprint values used earlier when the token was generated.

``` PowerShell
PS> $signingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
PS> $encryptingThumbprint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
```

Run the server:

``` PowerShell
PS> .\sestest server --connection $connectionThumbprint --sign $signingThumbprint --encrypt $encryptingThumbprint
```

The `--connection` option is mandatory because the server only works with an HTTPS connection. Omit the `--sign` and `--encrypt` options if you're not using secured tokens. For a full reference of all the available parameters for this mode, see [../src/sestest/readme.md](../src/sestest/readme.md).

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

Test the server with a web browser, connecting to the server by entering the URL shown on the console.

![Browser](../img/browser.png)

Only an HTTPS connection will work. The server does not listen for HTTP connections. If you specified any kind of locally signed certificate, you will likely need to override security features of your browser to connect.

Leave the server running in this shell window and return to the first. You may want to arrange the two shell windows so you can observe the one running the test server while you work in the original window.

In your original shell window, define an environment variable with the URL shown by the server:

``` PowerShell
$env:AZ_BATCH_ACCOUNT_URL = "https://localhost:4443"
```

## Verifying a token

To independently check a software entitlement token, use the `sesclient` console application. This allows you to check the consistency of the system without involving your application, helping you to identify the source of any issues. 

With the environment variables previously defined (`AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` and `AZ_BATCH_ACCOUNT_URL`), plus the certificate variable `$connectionThumbprint`, verify your token with this command:

``` PowerShell
PS> .\sesclient --url $env:AZ_BATCH_ACCOUNT_URL --thumbprint $connectionThumbprint --common-name localhost --token $env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN --application contosoapp
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

### Requested Operation is not supported

If you get an error that reads:

```
15:24:39.774 [Error] The requested operation is not supported
15:24:39.777 [Error] The requested operation is not supported (WindowsCryptographicException)
```

This means the certificate you've selected does not support encryption (this is a flag set on the certificate when it is created). You will need to select a different certificate.

## Suggested test cases

Once you have successfully integrated the software entitlement library into your application, test that your application does not run in each of the following cases:

* When no value is defined for the environment `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN`.
* When no value is defined for the environment variable `AZ_BATCH_ACCOUNT_URL`.
* When the server identified by `AZ_BATCH_ACCOUNT_URL` is unresponsive.
* When the server identified by `AZ_BATCH_ACCOUNT_URL` returns an error (5xx response code).
* When the token has already expired.
* When the token is not yet valid.
* When the token does not enable your application but does enable other applications.
* When the token enables your application on a machine with a different IP address.

