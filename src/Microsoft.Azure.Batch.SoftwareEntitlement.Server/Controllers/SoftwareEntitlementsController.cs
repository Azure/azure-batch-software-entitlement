using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers
{
    [Produces("application/json")]
    [Route("softwareEntitlements")]
    public class SoftwareEntitlementsController : Controller
    {
        // A reference to our logger
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementsController"/> class
        /// </summary>
        /// <param name="logger">Reference to our logger for diagnostics.</param>
        public SoftwareEntitlementsController(ILogger logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult RequestEntitlement(
            [FromBody]SoftwareEntitlementRequest entitlementRequest)
        {
            _logger.LogInformation($"Request entitlement for {entitlementRequest.ApplicationId}");
            _logger.LogDebug($"Application id: {entitlementRequest.ApplicationId}");
            _logger.LogDebug($"Request token: {entitlementRequest.Token}");

            // Hard coded for now, will use certificates later on
            var plainTextSecurityKey = "This is my shared, not so secret, secret!";
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSecurityKey));

            var verifier = new TokenVerifier(signingKey);
            var verificationResult = verifier.Verify(entitlementRequest.Token, entitlementRequest.ApplicationId);
            if (!verificationResult.HasValue)
            {
                foreach (var e in verificationResult.Errors)
                {
                    _logger.LogError(e);
                }

                var error = new SoftwareEntitlementFailureResponse
                {
                    Code = "EntitlementDenied",
                    Message = new ErrorMessage($"Entitlement for {entitlementRequest.ApplicationId} was denied.")
                };

                return StatusCode(403, error);
            }

            var entitlement = verificationResult.Value;

            var entitlementId = entitlementRequest.ApplicationId + "-" + Guid.NewGuid().ToString("D");
            var response = new SoftwareEntitlementSuccessfulResponse
            {
                EntitlementId = entitlementId,
                VirtualMachineId = entitlement.VirtualMachineId
            };

            return Ok(response);
        }
    }
}
