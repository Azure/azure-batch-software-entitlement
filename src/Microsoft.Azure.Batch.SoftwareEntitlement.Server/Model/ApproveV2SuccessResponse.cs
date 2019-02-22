using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    /// <summary>
    /// A response for a successful V1 approval request
    /// </summary>
    public class ApproveV2SuccessResponse : IResponseValue
    {
        /// <summary>
        /// The identifier for the approved entitlement
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string EntitlementId { get; set; }

        /// <summary>
        /// Time-stamp of the token's expiry (UTC)
        /// </summary>
        /// <remarks>
        /// This allows consuming software packages (e.g. schedulers) to make decisions based on 
        /// how long the token has yet to live.
        /// </remarks>
        [JsonProperty(PropertyName = "expiry", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset Expiry { get; set; }
    }
}
