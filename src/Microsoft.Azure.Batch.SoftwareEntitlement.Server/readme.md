# REST API for the Software Entitlement Service

Reference documentation for the current version of the REST API supported by the Software Entitlement Service. For historical versions and a list of changes, see the end of this document.

:warning: **This is draft documentation for the next version of the SES REST API** - see the [previous version](rest-api-2017-05-01.5.0.md) for the current release.

## Entitlement Acquisition

To verify the supplied token is genuine and permits ("entitles") the current application to execute, the application will contact the Azure Batch Software Entitlement Service, requesting an entitlement.

### Request

| Method | Request URI                                           |
|--------|-------------------------------------------------------|
| POST   | {endpoint}/softwareEntitlements?api-version={version} |

| Placeholder | Type   | Description                                                                                                                                                                                                                                                                                               |
|-------------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| endpoint    | string | The Batch account URL endpoint supplied by Azure Batch via the environment variable AZ_BATCH_ACCOUNT_URL. <br/>This may have a trailing `/` ("https://demo.westus.batch.azure.com/") or it might not ("https://demo.westus.batch.azure.com"); clients should be prepared to handle either case correctly. |
| version     | string | The API version of the request.                                                                                                                                                                                                                                                                           |

The payload for the request is a JSON document:

``` json
{
    "token": "...",
    "applicationId": "contosoapp",
    "applicationVersion": "2018.4",
    "duration": "PT5M",
    "metering": [{
        "type": "cpu",
        "count": 16
    }]
}
```

| Element            | Required  | Type           | Description                                                                                                                                                                                                                                                                                                                                           |
|--------------------|-----------|----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| token              | Mandatory | string         | The software entitlement token supplied to the application via environment variable from Azure Batch.                                                                                                                                                                                                                                                 |
| applicationId      | Mandatory | string         | A unique identifier for the application requesting an entitlement to run. <br/> Samples: contosoapp, application <br/> Application identifiers are lowercase alphanumeric (though comparisons will be case-insensitive), with no punctuation, whitespace or non-alphanumeric characters. <br/> Must be an application entitled by the token provided. |
| applicationVersion | Optional  | string         | The version of the application making the request. <br/> Sample: 2018.4<br/> Maximum length: 64                                                                                                                                                                                                                                                       |
| duration           | Mandatory | string         | The duration requested for the initial entitlement. <br/> Specified as ISO8601 duration.<br/> Minimum 5 minutes (PT5M), maximum 1 hour (PT1H).                                                                                                                                                                                                       |
| metering           | Optional  | array of Meter | An array of meters, defining how use of this application should be billed.<br/> If omitted, application billing will be based on total execution time.                                                                                                                                                                                                |

#### Meter

| Element | Required  | Type    | Description                                                                                                                                                                                                                               |
|---------|-----------|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| type    | Mandatory | string  | The kind of meter that should be used for billing of the application. <br/> Must be either “cpu” or “gpu” (but other values may be added in the future).                                                                                  |
| subType | Optional  | string  | Used to identify a meter variation within the specified type. Exact values will be agreed in advance on a per application basis with the vendor. <br/> E.g. for type “gpu”, this might specify the GPU family (“P40”, “K80”, “V100” etc). |
| count   | Mandatory | integer | The number of the specified items that should be billed.                                                                                                                                                                                  |

### Response 200 – OK

Verification of the token results in approval of the request and an entitlement is granted for the requested duration.

``` json 
{
    "entitlementId": "...",
    "expiryTime": "..."
}
```

| Element       | Required  | Type   | Description                                                                                                                                                                                                                                                             |
|---------------|-----------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| entitlementId | Mandatory | string | A unique identifier for the entitlement. This will remain constant for the duration of the leased entitlement. <br/> The format of this string is implementation dependent and client applications should treat it as an opaque identifier. The value will be URL safe. |
| expiryTime    | Mandatory | string | The instant at which the lease will expire, expressed as the time on the server (which might differ from the local time on the client due to clock skew). <br/> Formatted to ISO-8601 in UTC.                                                                           |

### Response 403 – Forbidden

The supplied software entitlement token does not entitle the application to run at this time.

The following example shows a sample JSON response:

``` json
{
    "code": "SoftwareEntitlementRequestDenied",
    "message":
    {
        "lang": "en-us",
        "value": "The software entitlement request was denied."
    },
    "values": [
        {
            "key": "Reason",
            "value": "Token does not grant entitlement for contosoapp."
        }
    ]
}
```

| Element | Required  | Type         | Notes                                                                     |
|---------|-----------|--------------|---------------------------------------------------------------------------|
| code    | Mandatory | string       | An error code with the constant value “SoftwareEntitlementRequestDenied”. |
| message | Mandatory | Complex Type | A human readable message describing the error.                            |

### Response 429 – Too Many Requests

The server rejected the request due to high load. Use the `Retry-After` header value to schedule a retry.
If a software entitlement token cannot be acquired after several retries, the package should behave as unlicensed.

### Response 400 – Bad Request

A mandatory element is missing, or some other error has occurred.
The following example shows a sample JSON response:

``` json
{
    "code": "MissingRequiredProperty",
    "message":
    {
        "lang": "en-us",
        "value": "A required property was not specified in the request body"
    },
    "values": [
        {
            "key": "PropertyName",
            "value": "applicationId"
        }
    ]
}
```

| Element | Required  | Type         | Notes                                              |
|---------|-----------|--------------|----------------------------------------------------|
| code    | Mandatory | string       | A code indicating the kind of error.               |
| message | Mandatory | Complex Type | A human readable message describing the error.     |
| values  | Optional  | array        | The properties of the request that were incorrect. |

Some example code values (not exhaustive):

| Code                          | Description                                                                                                                           |
|-------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| InvalidRequestBody            | The JSON content of the request was malformed, for example it contained additional/misspelt properties, or values of incorrect types. |
| InvalidPropertyValue          | Any of the required properties in the request were empty or whitespace.                                                               |
| MissingRequiredProperty       | Any of the required properties in the request were not supplied.                                                                      |
| InvalidQueryParameterValue    | The api-version query parameter value was not recognized.                                                                             |
| MissingRequiredQueryParameter | The api-version query parameter was not supplied.                                                                                     |
| InvalidUri                    | The URL was incorrect.                                                                                                                |
| InvalidHeaderValue            | The Content-Type header was missing or incorrect.                                                                                     |

One common cause for a 400 response is a malformed URL caused by code that always adds a separating `/` to the end of the URL provided in `AZ_BATCH_ACCOUNT_URL` without checking to see if a trailing `/` is *already* present.

E.g. if `AZ_BATCH_ACCOUNT_URL` is set to `https://demo.westus.batch.azure.com/` and the client appends `/softwareEntitlements/?api-version=2017-99-99-9.9` then the resulting URL of `https://demo.westus.batch.azure.com//softwareEntitlements/?api-version=2017-99-99-9.9` (containing a double `//`) will be invalid.

## Entitlement Renewal
Once successfully obtained, a lease should be renewed on a regular cadence, prior to expiry.

### Request

| Method | Url                                                                         |
|--------|-----------------------------------------------------------------------------|
| POST   | {endpoint}/softwareEntitlements/{entitlementId}/renew?api-version={version} |

| Placeholder   | Type   | Description                                                                                                                                                                                                                                                                                           |
|---------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| endpoint      | string | The Batch account URL endpoint supplied by Azure Batch via the environment variable AZ_BATCH_ACCOUNT_URL. <br/> May have a trailing “/” ("https://demo.westus.batch.azure.com/") or it might not ("https://demo.westus.batch.azure.com"); clients should be prepared to handle either case correctly. |
| entitlementId | string | The unique identifier of the entitlement that was returned from the entitlement request.                                                                                                                                                                                                              |
| version       | string | The API version of the request.                                                                                                                                                                                                                                                                       |

The payload for the request is a JSON document:

``` json
{
    "duration": "PT5M"
}
```

| Element  | Required  | Type   | Description                                                                                                                                                                                                                                                                                           |
|----------|-----------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| duration | Mandatory | string | The duration for which to renew the entitlement. <br/> Specified as ISO8601 duration. <br/> Minimum value 5 minutes (PT5M), maximum value 1 hour (PT1H).<br/>If the application is unexpectedly terminated, customers will be billed until the end of this lease; we therefore recommend low values. |

### Response 200 – OK

The entitlement was renewed for the requested duration.

``` json
{
    "expiryTime": "..."
}
```

| Element    | Required  | Type   | Description                                                                                                                                                     |
|------------|-----------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| expiryTime | Mandatory | string | The instant at which the entitlement will expire, expressed as the time on the server (which might differ from the local time on the client due to clock skew). |

### Response 403 – Forbidden

The renewal was declined. The application should interpret this as a failed license check.

The following example shows a sample JSON response:

``` json
{
    "code": "SoftwareEntitlementRequestDenied",
    "message":
    {
        "lang": "en-us",
        "value": "Renewal of entitlement {entitlementId} was denied."
    }
}
```

| Element | Required  | Type         | Notes                                                                     |
|---------|-----------|--------------|---------------------------------------------------------------------------|
| code    | Mandatory | string       | An error code with the constant value “SoftwareEntitlementRequestDenied”. |
| message | Mandatory | Complex Type | A human readable message describing the error.                            |

### Response 404 – Not Found

The entitlement doesn’t exist. This can happen if the specified URL does not correctly identify an existing lease. The service will not return 404 if the lease has merely expired (but not yet been released).

The following example shows a sample JSON response:

``` json
{
    "code": "NotFound",
    "message":
    {
        "lang": "en-us",
        "value": "The entitlement {entitlementId} was not found"
    }
}
```

| Element | Required  | Type         | Notes                                             |
|---------|-----------|--------------|---------------------------------------------------|
| code    | Mandatory | string       | An error code with the constant value “NotFound”. |
| message | Mandatory | Complex Type | A human readable message describing the error.    |

### Response 409 – Conflict

The entitlement has already been released/deleted.

The response will contain no body.

### Response 429 – Too Many Requests

The server rejected the request due to high load. Use the Retry-After header value to schedule a retry.

As noted above, late renewal of the software entitlement (after expiry) will still succeed.

### Response 400 – Bad Request

The renewal request was badly formed, due to a missing or malformed payload or another reason.

The following example shows a sample JSON response:

``` json
{
    "code": "InvalidRequestBody",
    "message":
    {
        "lang": "en-us",
        "value": "The specified Request Body is not syntactically valid"
    },
    "values": [
        {
            "key": "Reason",
            "value": "The property 'lengthOfTime' does not exist on type 'Microsoft.Azure.Batch.Protocol.Entities.SoftwareEntitlementRenewalRequest'. Make sure to only use property values that are defined by the type."
        }
    ]
}
```

| Element name | Required  | Type         | Notes                                              |
|--------------|-----------|--------------|----------------------------------------------------|
| code         | Mandatory | string       | A code indicating the kind of error.               |
| message      | Mandatory | Complex Type | A human readable message describing the error.     |
| values       | Optional  | array        | The properties of the request that were incorrect. |

Possible error codes are similar to those for entitlement acquisition.

One common cause for a 400 response is a malformed URL caused by code that always adds a separating `/` to the end of the URL provided in `AZ_BATCH_ACCOUNT_URL` without checking to see if a trailing `/` is *already* present.

E.g. if `AZ_BATCH_ACCOUNT_URL` is set to `https://demo.westus.batch.azure.com/` and the client appends `/softwareEntitlements/?api-version=2017-99-99-9.9` then the resulting URL of `https://demo.westus.batch.azure.com//softwareEntitlements/?api-version=2017-99-99-9.9` (containing a double `//`) will be invalid.

## Entitlement Release

Release of a leased entitlement when the application has finished execution.

| Method | Url                                                                   |
|--------|-----------------------------------------------------------------------|
| DELETE | {endpoint}/softwareEntitlements/{entitlementId}?api-version={version} |

| Placeholder   | Type   | Description                                                                                                                                                                                                                                                                                           |
|---------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| endpoint      | string | The Batch account URL endpoint supplied by Azure Batch via the environment variable AZ_BATCH_ACCOUNT_URL. <br/> May have a trailing “/” ("https://demo.westus.batch.azure.com/") or it might not ("https://demo.westus.batch.azure.com"); clients should be prepared to handle either case correctly. |
| entitlementId | string | The unique identifier of the entitlement that was returned from the original acquisition request.                                                                                                                                                                                                     |
| version       | string | The API version of the request.                                                                                                                                                                                                                                                                       |

### Response 204 – No Content

The entitlement was properly released or has already been released. 
The response will contain no body.

### Response 404 – Not Found

The entitlement doesn’t exist. This will only happen if the entitlement *never* existed.

The following example shows a sample JSON response:

``` json
{
    "code": "NotFound",
    "message":
    {
        "lang": "en-us",
        "value": "The specified entitlement {entitlementId} was not found."
    }
}
```

| Element name | Required  | Type         | Notes                                             |
|--------------|-----------|--------------|---------------------------------------------------|
| code         | Mandatory | string       | An error code with the constant value “NotFound”. |
| message      | Mandatory | Complex Type | A human readable message describing the error.    |

### Response 429 – Too Many Requests

The server rejected the request due to high load. Use the Retry-After header value to schedule a retry.

If a software entitlement token cannot be successfully released after several retries, it is safe to just let it lapse.

### Response 400 – Bad Request

The release request was badly formed, due to a malformed URI or another reason.

The following example shows a sample JSON response:

``` json
{
    "code": "InvalidQueryParameterValue",
    "message":
    {
        "lang": "en-us",
        "value": "Value for one of the query parameters specified in the request URI is invalid."
    },
    "values": [
        {
            "key": "QueryParameterName",
            "value": "api-version"
        },
        {
            "key": "QueryParameterValue",
            "value": "2001-01-01.0.0"
        },
        {
            "key": "Reason",
            "value": "The specified api version string is invalid."
        }
    ]
}
```

| Element name | Required  | Type         | Notes                                              |
|--------------|-----------|--------------|----------------------------------------------------|
| code         | Mandatory | string       | A code indicating the kind of error.               |
| message      | Mandatory | Complex Type | A human readable message describing the error.     |
| values       | Optional  | array        | The properties of the request that were incorrect. |

Possible error codes are similar to those for entitlement acquisition. 

One common cause for a 400 response is a malformed URL caused by code that always adds a separating `/` to the end of the URL provided in `AZ_BATCH_ACCOUNT_URL` without checking to see if a trailing `/` is *already* present.

E.g. if `AZ_BATCH_ACCOUNT_URL` is set to `https://demo.westus.batch.azure.com/` and the client appends `/softwareEntitlements/?api-version=2017-99-99-9.9` then the resulting URL of `https://demo.westus.batch.azure.com//softwareEntitlements/?api-version=2017-99-99-9.9` (containing a double `//`) will be invalid.

## Historical Versions

For prior versions of the REST API, see the following pages:

* Initial release, July 2017: [API Version 2017-05-01.5.0](rest-api-2017-05-01.5.0.md)

