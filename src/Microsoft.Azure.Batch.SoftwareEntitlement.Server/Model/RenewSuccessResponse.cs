using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    public class RenewSuccessResponse : IResponseValue
    {
        public RenewSuccessResponse(DateTime expiryTime)
        {
            ExpiryTime = expiryTime.ToUniversalTime().ToString("O");
        }

        /// <summary>
        /// The new expiry time of the entitlement in ISO-8601 format
        /// </summary>
        public string ExpiryTime { get; }
    }
}
