using System;
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
        [JsonProperty(PropertyName = "id")]
        public string EntitlementId { get; set; }

        /// <summary>
        /// [Deprecated] The virtual machine identifier for the Azure VM entitled to run the software
        /// </summary>
        [JsonProperty(PropertyName = "vmid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VirtualMachineId { get; set; }

        /// <summary>
        /// Time-stamp of the token's expiry
        /// </summary>
        /// <remarks>
        /// This allows consuming software packages (e.g. schedulers) to make decisions based on 
        /// how long the token has yet to live
        /// </remarks>
        [JsonProperty(PropertyName = "expiry", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset Expiry { get; set; }
    }
}
