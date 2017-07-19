# Sestest command line utility

This command line utility is provided to ease the task of integrating a software package with the Software Entitlement Service.

The `sestest` command line tool has multiple modes, as follows:

| Mode              | Use                                                                                                                                                                                                                                           |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| server            | Run a standalone software entitlement server, able to accept and verify tokens submitted by either the software vendor's application or `sestest verify`. This allows full testing of the application integration.                            |
| generate          | Generate a software entitlement token to enable the use of a specific package. This allows a vendor to generate a token for use during testing that conforms to the expected format and that will be correctly processed by `sestest server`. |
| list-certificates | List candidate certificates to use for testing <p/> This lists certificates that have private keys that may be suitable for use with HTTPS.                                                                                                   |
| find-certificate  | Find a specific certificate and show details.                                                                                                                                                                                                 |
| help              | Display more information on a specific command.                                                                                                                                                                                               |
| version           | Show version information.                                                                                                                                                                                                                     |

## Working with sestest 

To test that integration between an application and the software entitlement service is working correctly, you can run a local test as follows:

* Create a new token using `sestest generate` and store the generated token string as the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN`.
* Start `sestest server` in a separate console window to host a local software entitlement server. Keep this window visible so you can monitor the log output for diagnostic information.
* Set the environment variable `AZ_BATCH_ACCOUNT_URL` to `https://localhost:4443/`.
* Run the application requiring entitlement.

You should observe the request for entitlement being processed by the window running `sestest server`.

| Testing Scenario  | Notes                                                                                                                                                                                                                                                                                                    |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Sharing a server  | You can host the diagnostic software entitlement server (`sestest server`) anywhere on your network if the `AZ_BATCH_ACCOUNT_URL` environment variable on your test machine is updated to match. <p/> Note that logging goes to standard output by default.                                              |
| Token acceptance  | Use `sestest generate` to create a **valid** token and supply it to your application. The diagnostic software entitlement server should **accept** the token and the application should act as entitled.                                                                                                     |
| Token rejection   | Use `sestest generate` to create an **invalid** token (one that is not yet valid, one that is already expired, one for a different application, or one for a different computer) and supply it to your application. The diagnostic software entitlement server should **reject** the token and the application should act as non-entitled. |
| Automated testing | Passing the `--exit-after-request` parameter to `sestest server` will cause the server to cleanly exit after processing one request; this enables an automated integration test with your application.                                                                                                   |

## Common parameters

These parameters are available for every mode

| Parameter        | Required | Definition                                                                                                                 |
| ---------------- | -------- | -------------------------------------------------------------------------------------------------------------------------- |
| --log-level      | Optional | Specify the level of logging output (one of error, warning, information or debug). <p/> **Default**: Information.          |
| --log-file       | Optional | Sends all log messages into a file for later review.                                                                       |
| --log-file-level | Optional | Specify the level of logging output to the log file (one of error, warning, information or debug; defaults to --log-level) |
| --help           | Optional | Display this help screen.                                                                                                  |
| --version        | Optional | Display version information.                                                                                               |

## Standalone server

Run `sestest server` to stand up a diagnostic software entitlement server, able to accept and verify tokens submitted by either the application. This allows full testing of the integration.

**NOTE**: On Windows, ensure you run `sestest server` from an elevated shell window - this is required for certificate credential exchange to work. An error like _"The credentials supplied to the package were not recognized"_ may indicate that `sestest server` is running in a non-elevated shell window.

| Parameter            | Required  | Definition                                                                                                                              |
| -------------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| --connection         | Mandatory | Thumbprint of the certificate to use with HTTPS.                                                                                        |
| --url                | Optional  | The URL at which the server should process requests <br/> Defaults to `https://localhost:4443`; must start with `https:`.               |
| --sign               | Optional  | Thumbprint of the certificate used to sign tokens. <br/> If specified, only tokens signed with this certificate will be approved.       |
| --encrypt            | Optional  | Thumbprint of the certificate used to encrypt tokens. <br/> If specified, only tokens encrypted with this certificate will be approved. |
| --audience           | Optional  | Audience to which all tokens must be addressed. <br/> Defaults to `https://batch.azure.test/software-entitlement`.                      |
| --issuer             | Optional  | Issuer by which all tokens must have been created. <br/> Defaults to `https://batch.azure.test/software-entitlement`.                   |
| --exit-after-request | Optional  | ***PLANNED*** The server will exit after processing a single request.                                                                   |

You can see this documentation for yourself by running `sestest server --help` in your shell.

The exit code for `sestest server` will be zero (**0**) for normal exit of the server, non-zero (typically **-1**) if there were any command line parameter issues, if the server could not start or if the server crashes.

## Token generation

The `generate` mode allows you to generate a software entitlement token with the details required for your test scenario.

| Parameter     | Required  | Definition                                                                                                                                                                           |
| ------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| --application | Mandatory | Unique identifier(s) for the application(s) to include (comma separated).                                                                                                            |
| --vmid        | Mandatory | Unique identifier for the Azure virtual machine. If you are testing outside of Azure, we suggest you use the name of the machine (e.g. `%COMPUTERNAME%`).                            |
| --not-before  | Optional  | The moment at which the token becomes active and the application is entitled to execute <br/> Format: 'yyyy-mm-ddThh-mm'; 24 hour clock; local time; defaults to now.                |
| --not-after   | Optional  | The moment at which the token expires and the application is no longer entitled to execute <br/> Format: 'yyyy-mm-ddThh-mm'; 24 hour clock; local time; defaults to 7 days from now. |
| --address     | Optional  | The IP addresses of the machine entitled to execute the application(s). <br/> Defaults to all the IP addresses of the current machine.                                               |
| --audience    | Optional  | Audience to the token will be addressed. <br/> Defaults to `https://batch.azure.test/software-entitlement`.                                                                          |
| --issuer      | Optional  | Issuer by whom the tokens are created. <br/> Defaults to `http://batch.azure.test/software-entitlement`.                                                                             |
| --sign        | Optional  | Thumbprint of the certificate to use for signing the token                                                                                                                           |
| --encrypt     | Optional  | Thumbprint of the certificate to use for encryption of the token.                                                                                                                    |
| --token-file  | Optional  | The name of a file into which the token will be written <br/> If not specified, the token will be shown in the log.                                                                  |

You can see this documentation for yourself by running `sestest generate --help` in your console.

The exit code for `sestest generate` will be zero (**0**) if a token was correctly generated, non-zero (typically **-1**) if there were any issues.

**PowerShell users**: If you want to list multiple values for the `--application` parameter, wrap the entire list in double quotes to avoid PowerShell interpreting the comma (`,`) for array construction: `--application "app, app, app"`.

## List certificates

Lists available certificates that have private keys, providing an easy way to find the thumbprint of the certificate you need.

This mode has no additional options.

The exit code for `sestest list-certificates` will be zero (**0**) unless the application crashes.

## Find certificate

Find a given certificate given a thumbprint and show some details of that certificate.

| Parameter    | Required  | Definition                                         |
| ------------ | --------- | -------------------------------------------------- |
| --thumbprint | Mandatory | Thumbprint of the certificate to find and display. |

The exit code for `sestest find-certificate` will be zero (**0**) unless the application crashes.
