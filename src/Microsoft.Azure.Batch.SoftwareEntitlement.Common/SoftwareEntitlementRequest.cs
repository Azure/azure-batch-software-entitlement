namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A request for a software entitlement
    /// </summary>
    /// <remarks>Used to rehydrate the JSON body included with the POST requesting an entitlement.</remarks>
    public class SoftwareEntitlementRequest
    {
        /// <summary>
        /// The actual software entitlement token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Unique identifier for the application for which an entitlement is sought
        /// </summary>
        public string ApplicationId { get; set; }
    }
}
