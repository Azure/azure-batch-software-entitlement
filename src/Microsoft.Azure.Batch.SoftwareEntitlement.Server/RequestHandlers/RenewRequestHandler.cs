using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class RenewRequestHandler : IRequestHandler<(RenewRequestBody Body, string EntitlementId)>
    {
        private readonly ILogger _logger;
        private readonly EntitlementStore _entitlementStore;

        public RenewRequestHandler(
            ILogger logger,
            EntitlementStore entitlementStore)
        {
            _logger = logger;
            _entitlementStore = entitlementStore;
        }

        public Response Handle(
            HttpContext httpContext,
            (RenewRequestBody Body, string EntitlementId) requestContext)
        {
            var entitlementId = requestContext.EntitlementId;

            return requestContext.Body.Duration.ParseDuration().Match(
                whenSuccessful: duration => CheckEntitlementExists(entitlementId).Match(
                    whenSuccessful: existing => CheckNotReleased(entitlementId).Match(
                        whenSuccessful: notReleased =>
                        {
                            var expiry = StoreRenewal(entitlementId, duration);
                            return CreateSuccessResponse(expiry);
                        },
                        whenFailure: errors => errors.CreateConflictResponse("AlreadyReleased", _logger)),
                    whenFailure: errors => errors.CreateNotFoundResponse(entitlementId, _logger)),
                whenFailure: errors => errors.CreateBadRequestResponse(_logger));
        }

        private Errorable<object> CheckEntitlementExists(string entitlementId)
        {
            if (!_entitlementStore.ContainsEntitlementId(entitlementId))
            {
                return Errorable.Failure<object>($"Entitlement {entitlementId} not found");
            }

            return Errorable.Success<object>(null);
        }

        private Errorable<object> CheckNotReleased(string entitlementId)
        {
            if (_entitlementStore.IsReleased(entitlementId))
            {
                return Errorable.Failure<object>($"Entitlement {entitlementId} is already released");
            }

            return Errorable.Success<object>(null);
        }

        private DateTime StoreRenewal(string entitlementId, TimeSpan duration)
        {
            var expiryTime = DateTime.UtcNow.Add(duration);
            _entitlementStore.RenewEntitlement(entitlementId);
            return expiryTime;
        }

        private static Response CreateSuccessResponse(DateTime expiryTime)
        {
            var value = new RenewSuccessResponse(expiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
