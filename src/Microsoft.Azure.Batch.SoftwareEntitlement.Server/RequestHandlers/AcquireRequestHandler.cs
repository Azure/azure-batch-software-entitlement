using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class AcquireRequestHandler : RequestHandlerBase, IRequestHandler<AcquireRequestBody>
    {
        private readonly TokenVerifier _verifier;
        private readonly EntitlementStore _entitlementStore;

        public AcquireRequestHandler(
            ILogger logger,
            TokenVerifier tokenVerifier,
            EntitlementStore entitlementStore) : base(logger)
        {
            _verifier = tokenVerifier;
            _entitlementStore = entitlementStore;
        }

        public Response Handle(
            HttpContext httpContext,
            AcquireRequestBody requestContext)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

            return
            (
                from duration in ParseDuration(requestContext)
                from extracted in ExtractVerificationRequest(requestContext, remoteIpAddress)
                from tokenProperties in Verify(extracted.Request, extracted.Token)
                let entitlementId = CreateEntitlementId()
                let acquisitionTime = DateTime.UtcNow
                let expiry = acquisitionTime.Add(duration)
                let entitlementProperties = StoreEntitlement(entitlementId, tokenProperties, acquisitionTime)
                select CreateSuccessResponse(entitlementId, expiry)
            ).Merge();
        }

        private Result<TimeSpan, Response> ParseDuration(AcquireRequestBody body) =>
            body.Duration
                .ParseDuration()
                .OnError(CreateBadRequestResponse);

        private Result<(TokenVerificationRequest Request, string Token), Response> ExtractVerificationRequest(
            IVerificationRequestBody body,
            IPAddress remoteIpAddress) =>
            body.ExtractVerificationRequest(remoteIpAddress, Logger)
                .OnError(CreateBadRequestResponse);

        private Result<EntitlementTokenProperties, Response> Verify(
            TokenVerificationRequest request,
            string token) =>
            _verifier.Verify(request, token)
                .OnError(errors => CreateDeniedResponse(errors, request.ApplicationId));

        private static string CreateEntitlementId() => Guid.NewGuid().ToString("N");

        private EntitlementProperties StoreEntitlement(
            string entitlementId,
            EntitlementTokenProperties tokenProperties,
            DateTime acquisitionTime) =>
            _entitlementStore.StoreEntitlement(entitlementId, tokenProperties, acquisitionTime);

        private static Response CreateSuccessResponse(
            string entitlementId,
            DateTime expiryTime)
        {
            var value = new AcquireSuccessResponse(entitlementId, expiryTime);
            return Response.CreateSuccess(value);
        }
    }
}
