using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public class ApproveV1RequestHandler : RequestHandlerBase, IRequestHandler<ApproveRequestBody>
    {
        private readonly TokenVerifier _verifier;

        public ApproveV1RequestHandler(
            ILogger logger,
            TokenVerifier tokenVerifier) : base(logger)
        {
            _verifier = tokenVerifier;
        }

        public Response Handle(
            HttpContext httpContext,
            ApproveRequestBody requestContext)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

            return
            (
                from extracted in ExtractVerificationRequest(requestContext, remoteIpAddress)
                from props in Verify(extracted.Request, extracted.Token)
                select CreateSuccessResponse(props)
            ).Merge();
        }

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

        private static Response CreateSuccessResponse(EntitlementTokenProperties tokenProperties)
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
