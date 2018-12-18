using System;
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

            return requestContext.Duration.ParseDuration()
                .Bind(duration => Errorable.Success(duration).With(requestContext.ExtractVerificationRequest(remoteIpAddress, _logger)))
                .Map((duration, request, token) => (Duration: duration, Request: request, Token: token))
                .Match(
                    whenSuccessful: extracted => _verifier.Verify(extracted.Request, extracted.Token)
                        .Bind(verified => StoreEntitlement(extracted.Duration))
                        .Bind(stored => CreateSuccessResponse(stored.EntitlementId, stored.InitialExpiryTime))
                        .WhenFailure(errors => errors.CreateDeniedResponse(extracted.Request.ApplicationId, _logger)),
                    whenFailure: errors => errors.CreateBadRequestResponse(_logger));
        }

        private Errorable<(string EntitlementId, DateTime InitialExpiryTime)> StoreEntitlement(TimeSpan duration)
        {
            var entitlementId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            _entitlementStore.StoreEntitlementId(entitlementId);

            return Errorable.Success((entitlementId, now.Add(duration)));
        }

        private static Response CreateSuccessResponse(string entitlementId, DateTime initialExpiryTime)
        {
            var value = new AcquireSuccessResponse(entitlementId, initialExpiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
