# Software entitlement service testing utility (sestest)

This command line executable is intended to aid with testing of integration with the Azure Batch Software Entitlement Service.

**This is draft documentation subject to change.**

## Available Modes

| Mode                | Description                                                                 |
| ------------------- | --------------------------------------------------------------------------- |
| `generate`          | Generate a test software entitlement token with specified parameters        |
| `server`            | Run as a standalone software entitlement server for testing ISV integration |
| `list-certificates` | List candidate certificates to use for testing                              |
| `find-certificate`  | Find a specific certificate and show details                                |
| `help`              | Display more information on a specific command                              |
| `version`           | Show version information                                                    |

## Common parameters

These parameters are available for every mode

| Parameter                        | Required  | Definition                                                                                                           |
| -------------------------------- | --------- | -------------------------------------------------------------------------------------------------------------------- |
| `--log-level`                    | Optional  | Specify the level of logging output (one of error, warning, information or debug). <br/> Default value: Information. |
| `--help`                         | Optional  | Display this help screen.                                                                                            |
| `--version`                      | Optional  | Display version information.                                                                                         |

## Token Generation 

The `generate` mode allows you to generate a software entitlement token with the details required for your test scenario.

| Parameter                        | Required  | Definition                                                                                                                                                                                                                      |
| -------------------------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--entitlement-id <id> [, <id>]` | Mandatory | Unique identifier(s) for the entitlement(s) to include (comma separated).                                                                                                                                                       |
| `--vmid`                         | Mandatory | Unique identifier for the Azure virtual machine                                                                                                                                                                                 |
| `--not-before`                   | Optional  | The moment at which the token becomes active and the application is entitled to execute. <br/> Required format: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> Default value: Now.                                        |
| `--not-after`                    | Optional  | The moment at which the token expires and the application is no longer entitled to execute. <br/> Required format: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> Default value: 7 days (168 hours) after `--not-before`. |
| `--address`                      | Optional  | The externally visible IP address of the machine entitled to execute the application.                                                                                                                                           |
| `--sign`                         | Optional  | Certificate thumbprint of the certificate that should be used to sign the token.                                                                                                                                                |
| `--encrypt`                      | Optional  | Certificate thumbprint of the certificate that should be used to encrypt the token.                                                                                                                                             |
| `--token-file`                   | Optional  | The name of a file into which the token will be written (token will be written to stdout otherwise).                                                                                                                            |

## Standalone server

The `server` mode runs as a standalone software entitlement server for test purposes.

| Parameter                      | Required  | Definition                                                                                                                            |
| ------------------------------ | --------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `--connection-cert`            | Mandatory | Thumbprint of the certificate to pin for use with HTTPS                                                                               |
| `--url`                        | Optional  | The URL at which the server should process requests (defaults to https://localhost:4443). <br/> Validation: must start with 'https:'. |
| `--signing-cert`               | Optional  | Certificate thumbprint of the certificate used to sign the token                                                                      |
| `--encryption-cert`            | Optional  | Certificate thumbprint of the certificate used to encrypt the token                                                                   |



## Token Verification

```
sestest help verify

  --token                The token to verify

  --entitlement-id       Unique identifier for the entitlement to verify

  --vmid                 Unique identifier for the Azure virtual machine

  --batch-service-url    URL of the Azure Batch account server

  -a, --authority        Certificate thumbprint used to sign the cert used for the HTTPS connection
```


