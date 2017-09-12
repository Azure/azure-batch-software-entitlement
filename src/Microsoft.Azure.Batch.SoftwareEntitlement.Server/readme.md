# REST API for the Software Entitlement Service

Reference documentation for the current version of the REST API supported by the Software Entitlement Service. For historical versions and a list of changes, see the end of this document.

## Token Verification

Verifies that a provided software entitlement token grants permission to use a specific application.

### REQUEST

| Method | Request URI                                            |
| ------ | ------------------------------------------------------ |
| POST   | {endpoint}/softwareEntitlements/?api-version={version} |

Sample: `https://samples.westus.batch.azure.com/softwareEntitlements/?api-version=2017-99-99-9.9`

| Placeholder |  Type  |                                                                                                                                   Description                                                                                                                                    |
| ----------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| endpoint    | string | The Batch account URL endpoint supplied by Azure Batch via environment variable.                                                                                                                                                                                                 |
| version     | string | The API version of the request. <br/> Specify version `2017-99-99.9.9` or higher. <br/> For older API versions, see the end of this document. <br/> All Batch API versions are listed @ https://docs.microsoft.com/en-us/rest/api/batchservice/batch-service-rest-api-versioning |

The following shows a sample JSON payload for the request:

``` json
{
    "token": "...",
    "applicationId": "contosoapp"
}
```

| Element       | Required  | Type   | Description                                                                                                                                                                                                                                                                                |
| ------------- | --------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| token         | Mandatory | string | The software entitlement token supplied to the software package via environment variable from Azure Batch                                                                                                                                                                                  |
| applicationId | Mandatory | string | A unique identifier for the application requesting an entitlement to run. <br/> **Samples**: contosoapp, application <br/> Application identifiers are lowercase (though comparisons will be case-insensitive), with no punctuation, whitespace or non-alpha characters. |

Specific unique application identifiers for each software package will be agreed between Azure Batch and the software vendor in advance, prior to integration.

### RESPONSE 200 - OK

If the token grants permission to the requested application, the service will return HTTP Status 200 and the response body will contain details of the entitlement.

The following example shows a sample JSON response:

``` json
{
    "id": "entitlement-24223578-1CE8-4168-91E0-126C2D5EAA0B",
    "expiry": "2017-07-21T01:47:38.4420202Z"
}
```

| Element | Required  |   Type   |                                                                                                                                                                                                                   Description                                                                                                                                                                                                                   |
| ------- | --------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| id      | Mandatory | string   | A unique identifier for the specific entitlement issued to the application. <br/> Multiple entitlement requests for the same application from the same compute node may (but are not required to) return the same identifier. <br/> Entitlement requests from different compute nodes will not return duplicate identifiers. <br/> Clients should make no assumptions about the structure of the `id` as it may change from release to release. |
| expiry  | Mandatory | DateTime | The timestamp when the token expires. Once the token has expired, further verification requests will be declined.                                                                                                                                                                                                                                                                                                                               |

### RESPONSE 403 - FORBIDDEN

If the token does not grant permission to use the requested application, the service will return HTTP status 403 and the response body will contain extended error information.

An entitlement request may be denied if:

* The token has already expired;
* The requested application is not included in the token; or
* The token was issued to a different compute node.

The following example shows a sample JSON response:

``` json
{
    "code": "EntitlementDenied",
    "message":
    {
        "lang": "en-us",
        "value": "Software entitlement for 'contosoapp' was denied."
    }
}
```

See [Batch status and error codes](https://docs.microsoft.com/rest/api/batchservice/batch-status-and-error-codes) for more information.

### RESPONSE 400 - BAD REQUEST

The service will return HTTP status 400 and the response body will be empty if:

* The software entitlement token is missing, invalid, or corrupt;
* The request is badly formed; or
* The `api-version` specified on the URL is invalid.

## Historical Versions

For prior versions of the REST API, see the following pages:

* Initial release, July 2017: [API Version 2017-05-01.5.0](rest-api-2017-05-01.5.0.md)

### Changes in this version

Changes in this version of the REST API are:

* Removal of the `vmid` value from the response to a successful entitlement verification request.
* Addition of the `expiry` value to the response, giving visibility of the scheduled token expiry.

