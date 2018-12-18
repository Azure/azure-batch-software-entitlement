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

            return CheckEntitlementExists(entitlementId).Match(
                whenSuccessful: existing =>
                {
                    _entitlementStore.ReleaseEntitlement(entitlementId);
                    return CreateSuccessResponse();
                },
                whenFailure: errors => errors.CreateNotFoundResponse(entitlementId, _logger));
        }

        private Errorable<object> CheckEntitlementExists(string entitlementId)
        {
            if (!_entitlementStore.ContainsEntitlementId(entitlementId))
            {
                return Errorable.Failure<object>($"Entitlement {entitlementId} not found");
            }

            return Errorable.Success<object>(null);
        }

        private static Response CreateSuccessResponse() =>
            new Response(StatusCodes.Status204NoContent);
    }
}
