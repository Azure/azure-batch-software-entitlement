using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class ApproveV1RequestHandler : IRequestHandler<ApproveRequestBody>
    {
        private readonly ILogger _logger;
        private readonly TokenVerifier _verifier;

        public ApproveV1RequestHandler(
            ILogger logger,
            TokenVerifier tokenVerifier)
        {
            _logger = logger;
            _verifier = tokenVerifier;
        }

        public Response Handle(
            HttpContext httpContext,
            ApproveRequestBody requestBody)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
            return requestBody.ExtractVerificationRequest(remoteIpAddress, _logger).Match(
                whenSuccessful: r => CreateVerificationResponse(r.Request, r.Token),
                whenFailure: errors => errors.CreateBadRequestResponse(_logger));
        }

        private Response CreateVerificationResponse(TokenVerificationRequest request, string token)
        {
            return _verifier.Verify(request, token).Match(
                whenSuccessful: CreateSuccessResponse,
                whenFailure: errors => errors.CreateDeniedResponse(request.ApplicationId, _logger));
        }

        private Response CreateSuccessResponse(EntitlementTokenProperties tokenProperties)
        {
            var value = new ApproveV1SuccessResponse
            {
                // Return a value unique to this entitlement request, not the token identifier
                // (retaining the original format of having an 'entitlement-' prefix).
                EntitlementId = $"entitlement-{Guid.NewGuid()}",
                VirtualMachineId = tokenProperties.VirtualMachineId
            };

            return Response.CreateSuccess(value);
        }
    }
}
