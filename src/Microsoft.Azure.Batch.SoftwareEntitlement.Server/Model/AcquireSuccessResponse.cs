using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    public class AcquireSuccessResponse : IResponseValue
    {
        public AcquireSuccessResponse(string entitlementId, DateTime initialExpiryTime)
        {
            EntitlementId = entitlementId;
            InitialExpiryTime = initialExpiryTime.ToUniversalTime().ToString("O");
        }

        /// <summary>
        /// A unique identifier for the entitlement which will remain constant for the
        /// lifetime of the entitlement.
        /// </summary>
        public string EntitlementId { get; }

        /// <summary>
        /// The initial expiry time of the entitlement in ISO-8601 format
        /// </summary>
        public string InitialExpiryTime { get; }
    }
}
