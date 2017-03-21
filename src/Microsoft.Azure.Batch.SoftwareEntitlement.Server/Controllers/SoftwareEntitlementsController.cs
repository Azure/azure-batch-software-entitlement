using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

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

            /*
             * A simple hard coded rule for easy initial testing
             */
            if (!string.Equals(entitlementRequest.ApplicationId, "contosoapp", StringComparison.OrdinalIgnoreCase))
            {
                var error = new SoftwareEntitlementFailureResponse
                {
                    Code = "EntitlementDenied",
                    Message = new ErrorMessage($"Entitlements for {entitlementRequest.ApplicationId} not currently supported.")
                };

                return StatusCode(403, error);
            }

            var entitlementId = entitlementRequest.ApplicationId + "-" + Guid.NewGuid().ToString("D");
            var response = new SoftwareEntitlementSuccessfulResponse
            {
                EntitlementId = entitlementId,
                VirtualMachineId = Guid.NewGuid().ToString("B")
            };

            return Ok(response);
        }
    }
}
