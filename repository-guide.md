# Guide to the repository

The repository contains the software development kit (SDK) for the *Software Entitlement Service*, a feature of Azure Batch used to enable the execution of commercial software within Azure Batch.

## Documentation

The repository contains the following documentation

| Document                                                                                       | Content                                                                                                     |
| ---------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| [Repository guide](repository-guide.md)                                                        | This file, a summary of the content of this repository.                                                     |
| [Walk through](walkthrough.md)                                                                 | An introductory walk-through of the various components of the SDK, demonstrating their use and interaction. |
| [Static Library Readme](src\Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native\README.md) | Purpose and compilation of the embeddable static library                                                    |
| [Source Readme](src\readme.md)                                                                 | Development notes for extending the SDK                                                                     |
| [Native Client Readme](src\sesclient.native\README.md)                                         | Guidance for using the native client                                                                        |
| [Test tool Readme](src\sestest\readme.md)                                                      | Guidance for using the testing tool                                                                         |
| [Rest API Documentation](src\Microsoft.Azure.Batch.SoftwareEntitlement.Server\readme.md)       | Documentation of the REST API used                                                                          |

## Scripts

The root folder of the repository contains a number of convenience scripts intended to make it easier to work with the SDK.

| Script              | What it does                                            |
| ------------------- | ------------------------------------------------------- |
| `sestest.ps1`       | Runs the **sestest** tool.                              |
| `sesclient.ps1`     | Runs the **sesclient** tool.                            |
| `build-xplat.ps1`   | Builds the cross platform (.Net Core) components.       |
| `build-windows.ps1` | Builds the native components for use on Windows.        |
| `clean-build.ps1`   | Cleans away the results of prior builds.                |
| `test-coverage.ps1` | Run all unit tests and generate a test coverage report. |

## Folder Structure

The folders within the repository have the following uses.

| Folder    | What you will find                                                                                                                                                                                                                                                                                               |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src`     | Source for the linkable static library, the supporting tools `sestest` and `sesclient.native`, as well as supporting code.                                                                                                                                                                                       |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native` <br/> C++ source of the linkable static library that allows partner applications to verify they are running within a properly metered environment.                                                                                                     |
|           | `sesclient.native` <br/> A client console application (written in C++) that wraps the functionality of the static library, making it available outside a partner application for test purposes. This application also demonstrates how the static library should be used. |
|           | `sestest` <br/> A test console application (written in C#) that provides support for development and testing with the SDK, including the ability to create and verify new software entitlement tokens for testing.                                               |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement` <br/> A C# project containing key classes that implement functionality for token generation and verification.                                                                                                                                          |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement.Server` <br/> An ASP.NET Core server that acts as a software entitlement service endpoint suitable for testing.                                                                                                                                                       |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement.Common` <br/> Shared infrastructure code.                                                                                                                                                                                                                             |
| `tests`   | Source code for unit tests used to verify the correct functionality of the SDK                                                                                                                                                                                                                                   |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement.Tests` <br/> Unit tests of key functionality provided for token generation and verification. Includes tests to ensure that newly generated tokens are correctly processed and validated.                                                                              |
|           | `Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests` <br/> Unit tests of the shared infrastructure code.                                                                                                                                                                                                     |
| `img`     | Images that form a part of the documentation                                                                                                                                                                                                                                                                     |
| `scripts` | Various utility scripts used by the convenience scripts found in the root folder of the repository.                                                                                                                                                                                                              |

