# sestest Command Line utility

This commandline utility is provided to ease the task of integrating a software package with the Software Entitlement Service.

The `sestest` command line tool has multiple modes, as follows:

| Mode     | Use                                                                                                                                                                                                                                         |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| server   | Run a standalone software entitlement server, able to accept and verify tokens submitted by either the ISV application or `sestest verify`. This allows full testing of the ISV integration.                                                |
| generate | Generate a software entitlement token to enable the use of a specific package. This allows an ISV to generate a token for use during testing that conforms to the expected format and that will be correctly processed by `sestest server`. |

### Workflow

To test that integration between an ISV application and Azure Batch software entitlement service is working correctly, run a local test as follows:

* Create a new token using `sestest generate` and store the generated token string as the environment variable `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN`.
* Start `sestest server` in a separate console window to host a local software entitlement server. Keep this window visible so you can monitor the log output for diagnostic information.
* Set the environment variable `AZ_BATCH_ACCOUNT_URL` to `https://localhost:4443/`.
* Run the application requiring entitlement.

You should observe the request for entitlement being processed by the window running `sestest server`.

| Scenario                           | Notes                                                                                                                                                                                                                                                                                                                |
| ---------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Hosting `sestest server` elsewhere | You can host the diagnostic software entitlement server anywhere on your network if the AZ_BATCH_ACCOUNT environment variable on your test machine is updated to match. <p/> Note that doing this will make it harder to observe the diagnostic logging of the tool.                                                 |
| Generating an invalid token        | For testing purposes, you can use `sestest generate` to create a token that is not valid. <p/> For example, you could create a token that is not yet valid, one that is already expired, or one for a different application. In all cases, attempting to use an invalid token should result in a denied entitlement. |
| Automated testing                  | Passing the `--exit-after-request` parameter to `sestest server` will cause the server to cleanly exit after processing one request; this enables an automated integration test with your application.                                                                                                               |

## Available Modes

| Mode                | Description                                                                 |
| ------------------- | --------------------------------------------------------------------------- |
| generate            | Generate a test software entitlement token with specified parameters        |
| server              | Run as a standalone software entitlement server for testing ISV integration |
| list-certificates   | List candidate certificates to use for testing                              |
| find-certificate    | Find a specific certificate and show details                                |
| help                | Display more information on a specific command                              |
| version             | Show version information                                                    |

## Common parameters

These parameters are available for every mode

| Parameter                        | Required  | Definition                                                                                                           |
| -------------------------------- | --------- | -------------------------------------------------------------------------------------------------------------------- |
| --log-level                      | Optional  | Specify the level of logging output (one of error, warning, information or debug). <p/> **Default**: Information.    |
| --help                           | Optional  | Display this help screen.                                                                                            |
| --version                        | Optional  | Display version information.                                                                                         |

## Token Generation 

The `generate` mode allows you to generate a software entitlement token with the details required for your test scenario.

| Parameter         | Required  | Definition                                                                                                                                                                                                                        |
| ----------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| --entitlement-id  | Mandatory | Unique identifier(s) for the entitlement(s) to include (comma separated).                                                                                                                                                         |
| --vmid            | Mandatory | Unique identifier for the Azure virtual machine                                                                                                                                                                                   |
| --not-before      | Optional  | The moment at which the token becomes active and the application is entitled to execute. <p/> **Format**: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> **Default**: Now.                                                  |
| --not-after       | Optional  | The moment at which the token expires and the application is no longer entitled to execute. <p/> **Format**: `hh:mm d-mmm-yyyy`; 24 hour clock; local time. <br/> **Default**: 7 days (168 hours) after **--not-before**.         |
| --address         | Optional  | The externally visible IP address of the machine entitled to execute the application.                                                                                                                                             |
| --sign            | Optional  | Certificate thumbprint of the certificate that should be used to sign the token.                                                                                                                                                  |
| --encrypt         | Optional  | Certificate thumbprint of the certificate that should be used to encrypt the token.                                                                                                                                               |
| --token-file      | Optional  | The name of a file into which the token will be written (token will be written to stdout otherwise).                                                                                                                              |

## Standalone server

The `server` mode runs as a standalone software entitlement server for test purposes.

| Parameter                      | Required  | Definition                                                                                                                               |
| ------------------------------ | --------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| --connection-cert              | Mandatory | Thumbprint of the certificate to pin for use with HTTPS                                                                                  |
| --url                          | Optional  | The URL at which the server should process requests (defaults to https://localhost:4443). <p/> **Validation**: must start with `https:`. |
| --signing-cert                 | Optional  | Certificate thumbprint of the certificate used to sign the token                                                                         |
| --encryption-cert              | Optional  | Certificate thumbprint of the certificate used to encrypt the token                                                                      |



## Token Verification

```
sestest help verify

  --token                The token to verify

  --entitlement-id       Unique identifier for the entitlement to verify

  --vmid                 Unique identifier for the Azure virtual machine

  --batch-service-url    URL of the Azure Batch account server

  -a, --authority        Certificate thumbprint used to sign the cert used for the HTTPS connection
```


