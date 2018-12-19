using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class ReleaseRequestHandler : IRequestHandler<string>
    {
        private readonly ILogger _logger;
        private readonly EntitlementStore _entitlementStore;

        public ReleaseRequestHandler(
            ILogger logger,
            EntitlementStore entitlementStore)
        {
            _logger = logger;
            _entitlementStore = entitlementStore;
        }

        public Response Handle(
            HttpContext httpContext,
            string requestContext)
        {
            var entitlementId = requestContext;

            var responseOrNotFound =
                from exists in CheckEntitlementExists(entitlementId)
                let released = ReleaseEntitlement(entitlementId)
                select CreateSuccessResponse();

            return responseOrNotFound.OnFailure(GetNotFoundResponder(entitlementId));
        }

        private Errorable<bool> CheckEntitlementExists(string entitlementId)
        {
            if (!_entitlementStore.ContainsEntitlementId(entitlementId))
            {
                return Errorable.Failure<bool>($"Entitlement {entitlementId} not found");
            }

            return Errorable.Success(true);
        }

        private bool ReleaseEntitlement(string entitlementId)
        {
            _entitlementStore.ReleaseEntitlement(entitlementId);
            return true;
        }

        private Func<IEnumerable<string>, Response> GetNotFoundResponder(string entitlementId)
            => errors => errors.CreateNotFoundResponse(entitlementId, _logger);

        private static Response CreateSuccessResponse() =>
            new Response(StatusCodes.Status204NoContent);
    }
}
