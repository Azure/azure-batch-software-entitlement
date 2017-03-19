# Software Entitlement Service for Azure Batch

This SDK provides tooling and documentation to support ISV integration with the Software Entitlement Service of Azure Batch. This service allows a software package to verify it is running in an environment where usage metering takes place.

* The [software entitlement library code](src/Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native) is provided for integration into applications.  The interface is native C++, and depends on the openssl SDK for cross-platform support when communicating with the software entitlement server. 

* [sestest](src\sestest) command line utility is provided to assist with testing of the integration. This utility supports token generation and can run as a standalone software entitlement server for testing.

* The [REST API](src\Microsoft.Azure.Batch.SoftwareEntitlement.Server) details the interaction between the software application and the software entitlement server.

## Overview

<img src="img/interaction.png">

As a new pool is created within Azure Batch, details of the virtual machines are stored for later reference (1). When a task is scheduled to be run within that pool, a software entitlement token will be passed to the compute node along with the appropriate endpoint address for verification (2). These details will be made available to the software package through well-known environment variables (3). After verifying the provided endpoint address, the software package will securely contact the software entitlement service (4) which will verify the software entitlement token (5) and return information to the software package (6) for final verification.

## Task Scheduling

When a task is scheduled on a compute node, two pieces of information will be provided to the software package through environment variables:

| Variable                              | Definition                                                                                                        |
| ------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `AZ_BATCH_ACCOUNT_URL`                | The uri of an endpoint for the batch service account. <p/> Sample: `https://{myaccount}.{region}.batch.azure.com` |
| `AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN` | An encrypted base 64 encoded string containing the actual software entitlement token.                             |

The software package will need to verify that the provided batch account endpoint must specifies a known host (such as https://batch.azure.com or one of the equivalents for national clouds) to be accepted; if it does not, the package must not run.

The software entitlement token will be an encrypted and signed JWT token containing information about the virtual machine, the task and the permitted software packages.

*The software package is not expected to decrypt or otherwise process this token aside from passing it to the Software Entitlement Service for verification.*

# Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
