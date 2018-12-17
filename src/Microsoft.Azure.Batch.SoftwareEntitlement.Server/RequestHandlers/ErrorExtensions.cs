using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public static class ErrorExtensions
    {
        public static Response CreateBadRequestResponse(
            this IEnumerable<string> errors,
            ILogger logger)
        {
            logger.LogErrors(errors);

            var message = string.Join("; ", errors);
            var value = new FailureResponse("EntitlementDenied", new ErrorMessage(message));
            return new Response(StatusCodes.Status400BadRequest, value);
        }

        public static Response CreateDeniedResponse(
            this IEnumerable<string> errors,
            string applicationId,
            ILogger logger)
        {
            logger.LogErrors(errors);

            var message = $"Entitlement for {applicationId} was denied.";
            var value = new FailureResponse("EntitlementDenied", new ErrorMessage(message));
            return new Response(StatusCodes.Status403Forbidden, value);
        }
    }
}
