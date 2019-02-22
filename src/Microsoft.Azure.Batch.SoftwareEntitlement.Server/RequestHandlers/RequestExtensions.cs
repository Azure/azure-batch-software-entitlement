using System;
using System.Net;
using System.Xml;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public static class RequestExtensions
    {
        public static Result<(TokenVerificationRequest Request, string Token), ErrorSet> ExtractVerificationRequest(
            this IVerificationRequestBody requestBody,
            IPAddress remoteAddress,
            ILogger logger)
        {
            if (requestBody == null)
            {
                return ErrorSet.Create(
                    "Missing request body from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.Token))
            {
                return ErrorSet.Create(
                    "Missing token from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.ApplicationId))
            {
                return ErrorSet.Create(
                    "Missing applicationId value from software entitlement request.");
            }

            logger.LogDebug("Remote Address: {Address}", remoteAddress);

            var request = new TokenVerificationRequest(requestBody.ApplicationId, remoteAddress);

            return (Request: request, requestBody.Token);
        }

        public static Result<TimeSpan, ErrorSet> ParseDuration(this string duration)
        {
            if (string.IsNullOrEmpty(duration))
            {
                return ErrorSet.Create("Value for duration was not specified.");
            }

            try
            {
                return XmlConvert.ToTimeSpan(duration);
            }
            catch (FormatException e)
            {
                return ErrorSet.Create($"Unable to parse duration {duration}: {e.Message}");
            }
        }
    }
}
