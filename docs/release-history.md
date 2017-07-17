# Release History

## July 2017

Critical (but small) fixes to the SDK.

* **Change**: Permit the inclusion of a path as part of the Azure Batch account URL.

* **Change**: Validate the Azure Batch URL to ensure it doesn't include any query parameters prior to use.

* **Fix**: The API version query parameter of the URL used to request software entitlement should have been specified as `api-version`.

* **Fix**: The API version passed on the URL requesting software entitlement should have been `2017-05-01.5.0`.

* **Fix**: The response from `sestest server` for a successful entitlement should use `vmid` to pass the identifier for the entitled virtual machine.

* **Fix**: Change the contentType header from `application-json`, to `application/json; odata=minimalmetadata` as required by the Azure Batch services.

## May 2017

Initial public release of the Software Entitlement Service SDK for Azure Batch, including:

* [Software entitlement library code](src/Microsoft.Azure.Batch.SoftwareEntitlement.Client.Native) for integration into applications.

* The [sestest](src/sestest) commandline testing tool.

* Reference documentation on the [REST API](src/Microsoft.Azure.Batch.SoftwareEntitlement.Server.

* [Guide to compilation](docs/build-guide.md).

* [Full walk-through](docs/walk-through.md) documentation.

