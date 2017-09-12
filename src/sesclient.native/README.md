# Software entitlement service test client (sesclient.native)

This command line executable demonstrates use of the [native-code software entitlement library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native).

**This is draft documentation subject to change.**

## Working with sesclient.native

This executable is intended to be used in conjunction with the [sestest command line utility](../sestest) to generate tokens.

The executable expects the following parameters (in any order):

|   Parameter   | Required  |                                                                   Definition                                                                    |
| ------------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| --url         | Mandatory | The URL of the server that will process our request. <br/> **Note**: must start with `https:`.                                                  |
| --token       | Mandatory | The token as returned by `sestest generate`.                                                                                                    |
| --application | Mandatory | Unique identifier for the application being requested.                                                                                          |
| --thumbprint  | Optional  | Thumbprint of an additional certificate to accept in the TLS certificate chain. <br/> **Note**: cannot be the thumbprint of a root certificate. |
| --common-name | Optional  | The common name of the certificate indicated by `--thumbprint`.                                                                                 |

The library includes a set of well known Microsoft intermediate certificates that are always accepted. The `--thumbprint` and `--common-name` options allow another certificate to be added into the set of acceptable certificates, allowing for local testing in environments without access to those Microsoft certificates.
