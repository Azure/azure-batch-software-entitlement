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
        [JsonProperty(PropertyName = "vmid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VirtualMachineId { get; set; }
    }
}
