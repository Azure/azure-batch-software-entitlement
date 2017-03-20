# REST API for the Software Entitlement Server

## Token Verification

The software package will contact the Azure Batch Software Entitlement Service, requesting verification of the token passed as an environment variable.

### REQUEST

| Method | Request URI                                            |
| ------ | ------------------------------------------------------ |
| POST   | {endpoint}/softwareEntitlements/?api-version={version} |

Sample: `https://{myaccount}.{region}.batch.azure.com/softwareEntitlements/?api-version={version}`

| Placeholder | Type           | Description                                                                 |
| ----------- | -------------- | --------------------------------------------------------------------------- |
| endpoint    | string         | The batch account endpoint supplied by Azure Batch via environment variable |
| version     | string         | The API version of the request <p/> **Sample**: 2017-01-01.3.1              |

The following shows a sample JSON payload for the request:
```
{
    "token": "...",
    "applicationId": "contosoapp"
}
```

| Element       | Required  | Type   | Description                                                                                                                                                                                                                                                                     |
| ------------- | --------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| token         | Mandatory | string | The software entitlement authentication token supplied to the software package via environment variable from Azure Batch                                                                                                                                                        |
| applicationId | Mandatory | string | The previously agreed unique identifier for the application requesting an entitlement to run. <p/> **Samples**: contosoapp, application <p/> Application identifiers are lowercase (though comparisons will be case-insensitive), with no punctuation, whitespace or non-alpha characters. |

### RESPONSE 200 - OK

Verification of the token results in approval of the entitlement.

The following example shows a sample JSON response:
```
{
    "id": "contosoapp-24223578-1CE8-4168-91E0-126C2D5EAA0B",
    "url": "https://demo.westus.batch.azure.com/software.entitlements/contosoapp-24223578-1CE8-4168-91E0-126C2D5EAA0B?api-version=2017-03-01.4.0",
    "vmid": "..."
}
```

| Element | Required  | Type   | Description                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ------- | --------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| id      | Mandatory | string | A unique identifier for the specific entitlement issued to the application. <p/> Multiple entitlement requests for the same application from the same compute node may (but are not required to) return the same identifier. <p/> Entitlement requests from different compute nodes will not return duplicate identifiers. </p> Clients should make no assumptions about the structure of the `id` as it may change from release to release. |
| url     | Mandatory | string | A unique URI for the specific entitlement issued to the application. <p/> This URI will be correctly formulated for use with with the *Token Release* method described below.                                                                                                                                                                                                                                                                |
| vmid    | Mandatory | string | The unique identifier of the entitled azure virtual machine. <p/> Clients may optionally check this matches the actual virtual machine identifier for the host machine.                                                                                                                                                                                                                                                                      |

Future versions of the service may extend this response with additional information, perhaps including an expiry timestamp.

### RESPONSE 400 - BAD REQUEST

The calling service is not entitled to use the specified software.

The following example shows a sample JSON response:
```
{
    "code": "EntitlementDenied",
    "message":
    {
        "lang": "en-us",
        "value": "Application 'contosoapp' is not entitled to execute"
    }
}
```

| Element | Required  | Type         | Description                                              |
| ------- | --------- | ------------ | -------------------------------------------------------- |
| code    | Mandatory | string       | An error code with a machine readable code for the error |
| message | Mandatory | Complex Type | Contains a message for human consumption                 |
| lang    | Mandatory | string       | Specifies the language used for the message              |
| value   | Mandatory | string       | The error message itself                                 |

## Token Release 

When the entitlement is no longer required, it may be released by another REST API call. Releasing an entitlement is optional as task duration is not used for billing purposes.

### Request

| Method | Request URI                                                                                   |
| ------ | --------------------------------------------------------------------------------------------- |
| DELETE | https://{myaccount}.{region}.batch.azure.com/software.entitlements/{id}?api-version={version} |

There is no body to the request. Deletion must be requested from the same IP address as the original entitlement request.

| Placeholder | Type   | Description                                                       |
| ----------- | ------ | ----------------------------------------------------------------- |
| myaccount   | string | Unique name of the batch account                                  |
| region      | string | Region in which the job is running                                |
| id          | string | A unique identifier for the specific entitlement that was granted |
| api-version | string | A date and version string specifying the expected API version     |

Note that the uri returned in response to the *Token Verification* request will be preformatted with exactly the required URI for this request; clients **will not need** to directly construct the URI following the above specification.

### Response 200 - Release Accepted

The entitlement has been successfully released

### Response 404 - Not Found

The entitlement was not recognized.

This error may occur if the release entitlement call originates from a different IP address to the original request for the entitlement. That is, "I can release my entitlements, but I can't release yours."
