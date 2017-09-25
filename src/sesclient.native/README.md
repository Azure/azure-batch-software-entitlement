# Software entitlement service test client (sesclient.native)

This command line executable demonstrates use of the [native-code software entitlement library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native).

**This is draft documentation subject to change.**

## Working with sesclient.native

This executable is intended to be used in conjunction with the [sestest command line utility](../sestest) to generate tokens.

The executable expects the following parameters (in any order):

|   Parameter   | Required  |                                                                                                      Definition                                                                                                       |
| ------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| --url         | Mandatory | The URL of the server that will process our request. <br/> **Note**: must start with `https:`.                                                                                                                        |
| --token       | Mandatory | The token as returned by `sestest generate`.                                                                                                                                                                          |
| --application | Mandatory | Unique identifier for the application being requested.                                                                                                                                                                |
| --thumbprint  | Optional  | Thumbprint of an additional certificate to accept in the TLS certificate chain of the HTTPS connection. <br/> **Note**: cannot be the thumbprint of a root certificate. <br/> Mandatory if `--common-name` specified. |
| --common-name | Optional  | The common name of the certificate indicated by `--thumbprint`. <br/> Mandatory if `--thumbprint` specified.                                                                                                          |

The software entitlements library authenticates a server by requiring an HTTPS connection secured by a certificate signed by Microsoft. The library includes the thumbprints of set of well known Microsoft intermediate certificates for this purpose. These certificates are only available in the production Azure environment. 

To facilitate testing, the `--thumbprint` and `--common-name` parameters allow another certificate to be added to the set of acceptable certificates.
