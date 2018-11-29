using System.Net;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// The information provided by (or inferred about) the application and machine making the request.
    /// </summary>
    /// <remarks>
    /// This doesn't include the token or any claims associated with it, which is assumed to have been
    /// provided by Batch and passed on untouched by the application. This is the data that is compared
    /// against the token's claims to determine whether the request is allowed.
    /// </remarks>
    public class TokenVerificationRequest
    {
        /// <summary>
        /// Initializes a new instance of an token verification request
        /// </summary>
        /// <param name="applicationId">The identifier for the application the entitlement is being requested for</param>
        /// <param name="ipAddress">The observed IP address of the machine requesting the entitlement</param>
        public TokenVerificationRequest(string applicationId, IPAddress ipAddress)
        {
            ApplicationId = applicationId;
            IpAddress = ipAddress;
        }

        /// <summary>
        /// The identifier the application for which the request is being made
        /// </summary>
        public string ApplicationId { get; }

        /// <summary>
        /// Address of the machine requesting token validation
        /// </summary>
        public IPAddress IpAddress { get; }
    }
}
