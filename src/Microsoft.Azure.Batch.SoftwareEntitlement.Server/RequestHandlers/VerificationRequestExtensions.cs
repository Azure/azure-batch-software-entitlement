using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public static class VerificationRequestExtensions
    {
        public static Errorable<(TokenVerificationRequest Request, string Token)> ExtractVerificationRequest(
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

            return Errorable.Success((Request: request, Token: requestBody.Token));
        }
    }
}
