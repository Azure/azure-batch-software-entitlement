using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class RenewRequestHandler : RequestHandlerBase, IRequestHandler<(RenewRequestBody Body, string EntitlementId)>
    {
        private readonly EntitlementStore _entitlementStore;

        public RenewRequestHandler(
            ILogger logger,
            EntitlementStore entitlementStore) : base(logger)
        {
            _entitlementStore = entitlementStore;
        }

        public Response Handle(
            HttpContext httpContext,
            (RenewRequestBody Body, string EntitlementId) requestContext)
        {
            var entitlementId = requestContext.EntitlementId;

            return
            (
                from duration in ParseDuration(requestContext.Body)
                from foundEntitlement in FindEntitlement(entitlementId)
                where NotReleased(foundEntitlement)
                let renewalTime = DateTime.UtcNow
                let expiry = renewalTime.Add(duration)
                from renewedEntitlement in StoreRenewal(entitlementId, renewalTime)
                select CreateSuccessResponse(expiry)
            ).Merge();
        }

        private Result<TimeSpan, Response> ParseDuration(RenewRequestBody body) =>
            body.Duration
                .ParseDuration()
                .OnError(CreateBadRequestResponse);

        private Result<EntitlementProperties, Response> FindEntitlement(string entitlementId) =>
            _entitlementStore.FindEntitlement(entitlementId)
                .OnError(errors => CreateNotFoundResponse(entitlementId));

        private PredicateResult<Response> NotReleased(EntitlementProperties entitlementProperties) =>
            entitlementProperties.IsReleased.AsPredicateFailure(
                () => CreateAlreadyReleasedResponse(entitlementProperties.EntitlementId));

        private Result<EntitlementProperties, Response> StoreRenewal(string entitlementId, DateTime renewalTime) =>
            _entitlementStore.RenewEntitlement(entitlementId, renewalTime)
                .OnError(CreateInternalErrorResponse);

        private static Response CreateSuccessResponse(DateTime expiryTime)
        {
            var value = new RenewSuccessResponse(expiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
