# Software entitlement service test client (sesclient.native)

This command line executable demonstrates use of the [native-code software entitlement library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native).

**This is draft documentation subject to change.**

## Working with sesclient.native
This executable is intended to be used in conjunction with the [sestest command line utility](../sestest) to generate tokens.

The executable expects the following parameters (in any order):

| Parameter            | Required  | Definition                                                                                                                         |
| -------------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| --url                | Mandatory | The URL at which the server should process requests. <br/> **Note**: must start with `https:`.                                      |
| --thumbprint         | Mandatory | Thumbprint of a certificatein the server's SSL certificate chain. <br/> **Note**: cannot be the thumbprint of the root certificate. |
| --common-name        | Mandatory | The common name of the certificate indicated by `--thumbprint`.                                                                    |
| --token              | Mandatory | The token as returned by `sestest generate`.                                                                                       |
| --application        | Mandatory | Unique identifier for the application being requested.                                                                             |
