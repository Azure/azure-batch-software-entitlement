using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class ApproveV2RequestHandler : IRequestHandler<ApproveRequestBody>
    {
        private readonly ILogger _logger;
        private readonly TokenVerifier _verifier;

        public ApproveV2RequestHandler(
            ILogger logger,
            TokenVerifier tokenVerifier)
        {
            _logger = logger;
            _verifier = tokenVerifier;
        }

        public Response Handle(
            HttpContext httpContext,
            ApproveRequestBody requestContext)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
            return requestContext.ExtractVerificationRequest(remoteIpAddress, _logger).Match(
                whenSuccessful: extracted => _verifier.Verify(extracted.Request, extracted.Token)
                    .Bind(CreateSuccessResponse)
                    .WhenFailure(errors => errors.CreateDeniedResponse(extracted.Request.ApplicationId, _logger)),
                whenFailure: errors => errors.CreateBadRequestResponse(_logger));
        }

        private static Response CreateSuccessResponse(EntitlementTokenProperties tokenProperties)
        {
            var value = new ApproveV2SuccessResponse
            {
                // Return a value unique to this entitlement request, not the token identifier
                // (retaining the original format of having an 'entitlement-' prefix).
                EntitlementId = $"entitlement-{Guid.NewGuid()}",
                Expiry = tokenProperties.NotAfter
            };

            return Response.CreateSuccess(value);
        }
    }
}
