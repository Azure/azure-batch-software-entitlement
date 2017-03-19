using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A response for a successful request for a software entitlement
    /// </summary>
    public class SoftwareEntitlementSuccessfulResponse
    {
        /// <summary>
        /// The identifier for the approved entitlement
        /// </summary>
        /// <remarks>
        /// Will be formatted as a URL suitable for use for releasing the entitlement.
        /// </remarks>
        [JsonProperty(PropertyName = "id")]
        public string EntitlementId { get; set; }

        /// <summary>
        /// The url to use for management of the approved entitlement
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string EntitlementUrl { get; set; }

        /// <summary>
        /// The virtual machine identifier for the Azure VM entitled to run the software
        /// </summary>
        /// <remarks>
        /// This may be verified by the calling software package as an (optional) additional check.
        /// </remarks>
        [JsonProperty(PropertyName = "vmId")]
        public string VirtualMachineId { get; set; }
    }
}
