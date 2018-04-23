using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server
{
    /// <summary>
    /// A response for a failed request for a software entitlement
    /// </summary>
    public class SoftwareEntitlementFailureResponse
    {
        /// <summary>
        /// Gets or sets a unique machine readable identifier for the reason the entitlement was rejected
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets a human readable message detailing the reason the entitlement was rejected
        /// </summary>
        public ErrorMessage Message { get; set; }
    }
}
