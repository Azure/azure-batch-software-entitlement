using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    /// <summary>
    /// A response for a successful V1 approval request
    /// </summary>
    public class ApproveV1SuccessResponse : IResponseValue
    {
        /// <summary>
        /// The identifier for the approved entitlement
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string EntitlementId { get; set; }

        /// <summary>
        /// [Deprecated] The virtual machine identifier for the Azure VM entitled to run the software
        /// </summary>
        /// <remarks>
        /// The real Azure Batch implementation of this returns a random GUID rather than a meaningful
        /// value, so clients are not advised to make use of this value.
        /// </remarks>
        [JsonProperty(PropertyName = "vmid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VirtualMachineId { get; set; }
    }
}
