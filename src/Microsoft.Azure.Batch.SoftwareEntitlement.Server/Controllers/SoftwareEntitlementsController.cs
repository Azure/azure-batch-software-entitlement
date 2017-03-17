using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
        public object RequestEntitlement(
            [FromBody]SoftwareEntitlementRequest entitlementRequest)
        {
            _logger.LogInformation($"Request entitlement for {entitlementRequest.ApplicationId}");
            _logger.LogDebug($"Application id: {entitlementRequest.ApplicationId}");
            _logger.LogDebug($"Request token: {entitlementRequest.Token}");

            /*
             * A simple hard coded rule for easy initial testing
             */
            if (!string.Equals(entitlementRequest.ApplicationId, "maya", StringComparison.OrdinalIgnoreCase))
            {
                return new SoftwareEntitlementFailureResponse
                {
                    Code = "EntitlementDenied",
                    Message = new ErrorMessage($"Entitlements for ${entitlementRequest.ApplicationId} not currently supported.")
                };
            }

            var request = HttpContext.Request;
            var entitlementId = request.GetEncodedUrl() + "/" + Guid.NewGuid().ToString("D");

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
