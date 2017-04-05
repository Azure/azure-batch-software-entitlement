namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A response for a failed request for a software entitlement
    /// </summary>
    public class SoftwareEntitlementFailureResponse
    {
        /// <summary>
        /// A unique machine readable identifier for the reason the entitlement was rejected
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// A human readable message detailing the reason the entitlement was rejected
        /// </summary>
        public ErrorMessage Message { get; set; }
    }
}
