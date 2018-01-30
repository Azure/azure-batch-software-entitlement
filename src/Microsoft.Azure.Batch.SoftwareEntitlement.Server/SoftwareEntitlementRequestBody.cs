namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server
{
    /// <summary>
    /// A request for a software entitlement
    /// </summary>
    /// <remarks>Used to rehydrate the JSON body included with the POST requesting an entitlement.</remarks>
    public class SoftwareEntitlementRequestBody
    {
        /// <summary>
        /// The actual software entitlement token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Unique identifier for the application for which an entitlement is sought
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// A unique identifier for the host machine
        /// </summary>
        public string HostId { get; set; }

        /// <summary>
        /// The number of CPU cores claimed to be on the host machine, or null if no value was supplied
        /// </summary>
        public int? Cores { get; set; }
    }
}
