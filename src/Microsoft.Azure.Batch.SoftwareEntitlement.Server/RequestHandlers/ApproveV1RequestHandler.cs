using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
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
            ApproveRequestBody requestContext)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

            var responseOrBadRequest =
                from extracted in requestContext.ExtractVerificationRequest(remoteIpAddress, _logger)
                let responseOrDenied =
                    from props in _verifier.Verify(extracted.Request, extracted.Token)
                    select CreateSuccessResponse(props)
                select responseOrDenied.OnFailure(GetDeniedResponder(extracted.Request.ApplicationId));

            return responseOrBadRequest.OnFailure(GetBadRequestResponder());
        }

        private Func<IEnumerable<string>, Response> GetDeniedResponder(string applicationId)
            => errors => errors.CreateDeniedResponse(applicationId, _logger);

        private Func<IEnumerable<string>, Response> GetBadRequestResponder()
            => errors => errors.CreateBadRequestResponse(_logger);

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
