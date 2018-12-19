using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class AcquireRequestHandler : IRequestHandler<AcquireRequestBody>
    {
        private readonly ILogger _logger;
        private readonly TokenVerifier _verifier;
        private readonly EntitlementStore _entitlementStore;

        public AcquireRequestHandler(
            ILogger logger,
            TokenVerifier tokenVerifier,
            EntitlementStore entitlementStore)
        {
            _logger = logger;
            _verifier = tokenVerifier;
            _entitlementStore = entitlementStore;
        }

        public Response Handle(
            HttpContext httpContext,
            AcquireRequestBody requestContext)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

            var responseOrBadRequest =
                from duration in requestContext.Duration.ParseDuration()
                from extracted in requestContext.ExtractVerificationRequest(remoteIpAddress, _logger)
                let responseOrDenied =
                    from props in _verifier.Verify(extracted.Request, extracted.Token)
                    let stored = StoreEntitlement(duration)
                    select CreateSuccessResponse(stored.EntitlementId, stored.InitialExpiryTime)
                select responseOrDenied.OnFailure(GetDeniedResponder(extracted.Request.ApplicationId));

            return responseOrBadRequest.OnFailure(GetBadRequestResponder());
        }

        private (string EntitlementId, DateTime InitialExpiryTime) StoreEntitlement(TimeSpan duration)
        {
            var entitlementId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            _entitlementStore.StoreEntitlementId(entitlementId);

            return (entitlementId, now.Add(duration));
        }

        private Func<IEnumerable<string>, Response> GetDeniedResponder(string applicationId)
            => errors => errors.CreateDeniedResponse(applicationId, _logger);

        private Func<IEnumerable<string>, Response> GetBadRequestResponder()
            => errors => errors.CreateBadRequestResponse(_logger);

        private static Response CreateSuccessResponse(string entitlementId, DateTime initialExpiryTime)
        {
            var value = new AcquireSuccessResponse(entitlementId, initialExpiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
