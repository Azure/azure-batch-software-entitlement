namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    /// <summary>
    /// A request body which needs to be verified to request an entitlement.
    /// </summary>
    public interface IVerificationRequestBody
    {
        /// <summary>
        /// A software entitlement token
        /// </summary>
        string Token { get; }

        /// <summary>
        /// Unique identifier for the application for which an entitlement is sought
        /// </summary>
        string ApplicationId { get; }
    }
}
