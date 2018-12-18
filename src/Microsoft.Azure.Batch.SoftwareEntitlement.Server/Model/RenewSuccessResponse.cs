using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    public class RenewSuccessResponse : IResponseValue
    {
        public RenewSuccessResponse(DateTime expiryTime)
        {
            ExpiryTime = expiryTime.ToUniversalTime().ToString("O");
        }

        public string ExpiryTime { get; }
    }
}
