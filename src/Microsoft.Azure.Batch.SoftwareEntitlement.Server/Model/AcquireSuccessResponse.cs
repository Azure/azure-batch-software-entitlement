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

        public string EntitlementId { get; }

        public string InitialExpiryTime { get; }
    }
}
