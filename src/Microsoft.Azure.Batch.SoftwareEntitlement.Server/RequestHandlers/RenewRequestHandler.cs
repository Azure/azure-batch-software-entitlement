using System;
using System.Collections.Generic;
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

            var responseOrBadRequest =
                from duration in requestContext.Body.Duration.ParseDuration()
                let responseOrNotFound =
                    from exists in CheckEntitlementExists(entitlementId)
                    let responseOrConflict =
                        from notReleased in CheckNotReleased(entitlementId)
                        let expiry = StoreRenewal(entitlementId, duration)
                        select CreateSuccessResponse(expiry)
                    select responseOrConflict.OnFailure(GetAlreadyReleasedResponder())
                select responseOrNotFound.OnFailure(GetNotFoundResponder(entitlementId));

            return responseOrBadRequest.OnFailure(GetBadRequestResponder());
        }

        private Errorable<bool> CheckEntitlementExists(string entitlementId)
        {
            if (!_entitlementStore.ContainsEntitlementId(entitlementId))
            {
                return Errorable.Failure<bool>($"Entitlement {entitlementId} not found");
            }

            return Errorable.Success(true);
        }

        private Errorable<bool> CheckNotReleased(string entitlementId)
        {
            if (_entitlementStore.IsReleased(entitlementId))
            {
                return Errorable.Failure<bool>($"Entitlement {entitlementId} is already released");
            }

            return Errorable.Success(true);
        }

        private DateTime StoreRenewal(string entitlementId, TimeSpan duration)
        {
            var expiryTime = DateTime.UtcNow.Add(duration);
            _entitlementStore.RenewEntitlement(entitlementId);
            return expiryTime;
        }

        private Func<IEnumerable<string>, Response> GetAlreadyReleasedResponder()
            => errors => errors.CreateConflictResponse("AlreadyReleased", _logger);

        private Func<IEnumerable<string>, Response> GetNotFoundResponder(string entitlementId)
            => errors => errors.CreateNotFoundResponse(entitlementId, _logger);

        private Func<IEnumerable<string>, Response> GetBadRequestResponder()
            => errors => errors.CreateBadRequestResponse(_logger);

        private static Response CreateSuccessResponse(DateTime expiryTime)
        {
            var value = new RenewSuccessResponse(expiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
