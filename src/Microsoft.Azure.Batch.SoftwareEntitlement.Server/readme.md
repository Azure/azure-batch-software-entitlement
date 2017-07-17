# REST API for the Software Entitlement Service

## Token Verification

Verifies that a provided software entitlement token grants permission to use a specific application.

### REQUEST

| Method | Request URI                                            |
| ------ | ------------------------------------------------------ |
| POST   | {endpoint}/softwareEntitlements/?api-version={version} |

Sample: `https://samples.westus.batch.azure.com/softwareEntitlements/?api-version=2017-05-01.5.0`

| Placeholder | Type   | Description                                                                       |
| ----------- | ------ | --------------------------------------------------------------------------------- |
| endpoint    | string | The Batch account url endpoint supplied by Azure Batch via environment variable.  |
| version     | string | The API version of the request. <p/> Must be `2017-05-01.5.0` or `2017-06-01.5.1` |

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
| applicationId | Mandatory | string | A unique identifier for the application requesting an entitlement to run. <p/> **Samples**: contosoapp, application <p/> Application identifiers are lowercase (though comparisons will be case-insensitive), with no punctuation, whitespace or non-alpha characters. |

Specific unique application identifiers for each software package will be agreed between Azure Batch and the software vendor in advance, prior to integration.

### RESPONSE 200 - OK

If the token grants permission to the requested application, the service will return HTTP Status 200 and the response body will contain details of the entitlement.

The following example shows a sample JSON response:

``` json
{
    "id": "entitlement-24223578-1CE8-4168-91E0-126C2D5EAA0B",
    "vmid": "..."
}
```

| Element | Required  | Type   | Description                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| id      | Mandatory | string | A unique identifier for the specific entitlement issued to the application. <p/> Multiple entitlement requests for the same application from the same compute node may (but are not required to) return the same identifier. <p/> Entitlement requests from different compute nodes will not return duplicate identifiers. </p> Clients should make no assumptions about the structure of the `id` as it may change from release to release. |
| vmid    | Mandatory | string | The unique [virtual machine identifier](https://azure.microsoft.com/blog/accessing-and-using-azure-vm-unique-id/) of the entitled Azure virtual machine. <p/> Clients may optionally check this matches the actual virtual machine identifier for the host machine.                                                                                                                                                                          |

### RESPONSE 403 - FORBIDDEN

If the token does not grant permission to use the requested application, the service will return HTTP status 403 and the response body will contain extended error information.

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
