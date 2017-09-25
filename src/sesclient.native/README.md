# Software entitlement service test client (sesclient.native)eed

This command line executable demonstrates use of the [native-code software entitlement library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native).

**This is draft documentation subject to change.**

## Available Parameters

The executable expects the following parameters (in any order):

|   Parameter   | Required  |                                                                                                      Definition                                                                                                       |
| ------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| --url         | Mandatory | The URL of the server that will process our request. <br/> **Note**: must start with `https:`.                                                                                                                        |
| --token       | Mandatory | The token as returned by `sestest generate`.                                                                                                                                                                          |
| --application | Mandatory | Unique identifier for the application being requested.                                                                                                                                                                |
| --thumbprint  | Optional  | Thumbprint of an additional certificate to accept in the TLS certificate chain of the HTTPS connection. <br/> **Note**: cannot be the thumbprint of a root certificate. <br/> Mandatory if `--common-name` specified. |
| --common-name | Optional  | The common name of the certificate indicated by `--thumbprint`. <br/> Mandatory if `--thumbprint` specified.                                                                                                          |

## Examples

### Production

In the Azure Batch production environment, `sesclient.native` can be used as a stand-in for any other application that has been integrated with our software entitlements service. This gives the Azure Batch team a diagnostic tool that can be used to isolate issues.

To run in production:

``` cmd
sesclient.native --url %AZ_BATCH_ACCOUNT_URL% --token %AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN% --application contosoapp
```

The environment variables `AZ_BATCH_ACCOUNT_URL` and `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` are published by Azure Batch as a part of the environment on compute nodes.

In this scenario, `sesclient.native` will verify that the connection is made to a genuine Azure Batch server by checking the certificate used to secure the HTTPS connection. If the certificate used for the connection does not trace back to one of the well known Microsoft intermediate certificate authorities built into the [native-code library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native), the software entitlement check will fail.

### Testing

In a test environment, you won't have access to a certificate signed by any one of the well known Microsoft intermediate certificate authorities, so `sesclient.native` allows you to specify your own certificate to use when running outside of Azure Batch. 

You'll need to specify the same certificate for `sesclient.native` as you already do for `sestest server` so the two components can handshake the connection properly.

Assuming the appropriate thumbprint is available in the environment variable `%CONNECTION_THUMBPRINT%`, and the common name of that certificate is available in `%COMMON_NAME%`, run `sestest` as a server:

``` cmd
sestest server --connection %CONNECTION_THUMBPRINT% --common-name ...
```

(Note that )

You can now use `sesclient.native` to verify a token locally:

```
sesclient.native --url ... --token ... --application ... --thumbprint %CONNECTION_THUMBPRINT% --common-name %COMMON_NAME%
```

The `--thumbprint` and `--common-name` parameters identify your local certificate and configure the [native-code library](../Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native) to treat a server using that certificate as a genuine Azure Batch server.

## See Also

For more information, see our [step by step walk-through](..\..\docs\walk-through.md).
