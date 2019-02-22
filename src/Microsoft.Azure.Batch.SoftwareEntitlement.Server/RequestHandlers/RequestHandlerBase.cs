using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public abstract class RequestHandlerBase
    {
        protected RequestHandlerBase(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        protected Response CreateBadRequestResponse(IEnumerable<string> errors)
        {
            Logger.LogErrors(errors);

            var message = string.Join("; ", errors);
            var value = new FailureResponse("EntitlementDenied", new ErrorMessage(message));
            return new Response(StatusCodes.Status400BadRequest, value);
        }

        protected Response CreateDeniedResponse(
            IEnumerable<string> errors,
            string applicationId)
        {
            Logger.LogErrors(errors);

            var message = $"Entitlement for {applicationId} was denied.";
            var value = new FailureResponse("EntitlementDenied", new ErrorMessage(message));
            return new Response(StatusCodes.Status403Forbidden, value);
        }

        protected Response CreateNotFoundResponse(string entitlementId)
        {
            var message = $"Entitlement {entitlementId} was not found.";
            Logger.LogError(message);
            var value = new FailureResponse("NotFound", new ErrorMessage(message));
            return new Response(StatusCodes.Status404NotFound, value);
        }

        protected Response CreateAlreadyReleasedResponse(string entitlementId)
        {
            var message = $"Entitlement {entitlementId} is already released";
            Logger.LogError(message);
            var value = new FailureResponse("AlreadyReleased", new ErrorMessage(message));
            return new Response(StatusCodes.Status409Conflict, value);
        }

        protected Response CreateInternalErrorResponse(IEnumerable<string> errors)
        {
            Logger.LogErrors(errors);

            var message = string.Join("; ", errors);
            var value = new FailureResponse("InternalServerError", new ErrorMessage(message));
            return new Response(StatusCodes.Status500InternalServerError, value);
        }
    }
}
