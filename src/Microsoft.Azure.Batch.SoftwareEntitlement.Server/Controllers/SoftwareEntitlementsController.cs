using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers
{
    [Produces("application/json")]
    [Route("software.entitlements")]
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

        [HttpPost("{applicationId}")]
        public object RequestEntitlement(
            [FromRoute]string applicationId,
            [FromBody]SoftwareEntitlementRequest entitlementRequest)
        {
            _logger.LogInformation($"Request entitlement for {applicationId}");
            _logger.LogDebug($"Application id: {applicationId}");
            _logger.LogDebug($"Request token: {entitlementRequest.Token}");

            /*
             * A simple hard coded rule for easy initial testing
             */
            if (!string.Equals(applicationId, "maya", StringComparison.OrdinalIgnoreCase))
            {
                return new SoftwareEntitlementFailureResponse
                {
                    Code = "NotMaya",
                    Message = new ErrorMessage($"Entitlements for ${applicationId} not currently supported.")
                };
            }

            var request = HttpContext.Request;
            var entitlementId =
                $"{request.Scheme}://{request.Host}{request.Path}/{Guid.NewGuid():D}";

            return new SoftwareEntitlementSuccessfulResponse
            {
                EntitlementId = entitlementId,
                VirtualMachineId = Guid.NewGuid().ToString("B")
            };
        }

        [HttpDelete("{applicationId}/{entitlementId}")]
        public void ReleaseEntitlement(
            [FromRoute] string applicationId,
            [FromRoute] string entitlementId)
        {
            _logger.LogInformation($"Release entitlement {entitlementId} for {applicationId}");
        }
    }
}
