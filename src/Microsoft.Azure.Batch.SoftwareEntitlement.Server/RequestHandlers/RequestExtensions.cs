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
        public static Result<(TokenVerificationRequest Request, string Token), ErrorCollection> ExtractVerificationRequest(
            this IVerificationRequestBody requestBody,
            IPAddress remoteAddress,
            ILogger logger)
        {
            if (requestBody == null)
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing request body from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.Token))
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing token from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.ApplicationId))
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing applicationId value from software entitlement request.");
            }

            logger.LogDebug("Remote Address: {Address}", remoteAddress);

            var request = new TokenVerificationRequest(requestBody.ApplicationId, remoteAddress);

            return Errorable.Success((Request: request, requestBody.Token));
        }

        public static Result<TimeSpan, ErrorCollection> ParseDuration(this string duration)
        {
            if (string.IsNullOrEmpty(duration))
            {
                return Errorable.Failure<TimeSpan>("Value for duration was not specified.");
            }

            try
            {
                var timeSpan = XmlConvert.ToTimeSpan(duration);
                return Errorable.Success(timeSpan);
            }
            catch (FormatException e)
            {
                return Errorable.Failure<TimeSpan>($"Unable to parse duration {duration}: {e.Message}");
            }
        }
    }
}
