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

| Parameter                        | Required  | Definition                                                                                                           |
| -------------------------------- | --------- | -------------------------------------------------------------------------------------------------------------------- |
| --log-level                      | Optional  | Specify the level of logging output (one of error, warning, information or debug). <p/> **Default**: Information.    |
| --help                           | Optional  | Display this help screen.                                                                                            |
| --version                        | Optional  | Display version information.                                                                                         |

## Standalone server

Run `sestest server` to stand up a diagnostic software entitlement server, able to accept and verify tokens submitted by either the application. This allows full testing of the integration.

| Parameter            | Required  | Definition                                                                                                                               |
| -------------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| --connection-cert    | Mandatory | Thumbprint of the certificate to pin for use with HTTPS                                                                                  |
| --url                | Optional  | The URL at which the server should process requests (defaults to https://localhost:4443). <p/> **Validation**: must start with `https:`. |
| --signing-cert       | Optional  | ***PLANNED*** Certificate thumbprint of the certificate used to sign the token                                                           |
| --encryption-cert    | Optional  | ***PLANNED*** Certificate thumbprint of the certificate used to encrypt the token                                                        |
| --exit-after-request | Optional  | ***PLANNED*** The server will exit after processing a single request.                                                                    |

The exit code for `sestest server` will be zero (**0**) for normal exit of the server, non-zero (typically **-1**) if there were any command line parameter issues, if the server could not start or if the server crashes.

## Token generation 

The `generate` mode allows you to generate a software entitlement token with the details required for your test scenario.

| Parameter     | Required  | Definition                                                                                                                                                                                                                |
| ------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| --application | Mandatory | Unique identifier(s) for the application(s) to include (comma separated).                                                                                                                                                 |
| --vmid        | Mandatory | Unique identifier for the Azure virtual machine                                                                                                                                                                           |
| --not-before  | Optional  | The moment at which the token becomes active and the application is entitled to execute. <p/> **Format**: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> **Default**: Now.                                          |
| --not-after   | Optional  | The moment at which the token expires and the application is no longer entitled to execute. <p/> **Format**: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> **Default**: 7 days (168 hours) after **--not-before**. |
| --address     | Optional  | ***PLANNED*** The externally visible IP address of the machine entitled to execute the application. <p/> **Default**: The IP address of the current machine.                                                              |
| --sign        | Optional  | ***PLANNED*** Certificate thumbprint of the certificate that should be used to sign the token.                                                                                                                            |
| --encrypt     | Optional  | ***PLANNED*** Certificate thumbprint of the certificate that should be used to encrypt the token.                                                                                                                         |
| --token-file  | Optional  | ***PLANNED***The name of a file into which the token will be written (token will be written to stdout otherwise).                                                                                                         |

The exit code for `sestest generate` will be zero (**0**) if a token was correctly generated, non-zero (typically **-1**) if there were any issues.

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

