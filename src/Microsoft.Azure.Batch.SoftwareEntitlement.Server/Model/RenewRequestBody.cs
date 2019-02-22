namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    /// <summary>
    /// A request for a leased software entitlement renewal
    /// </summary>
    /// <remarks>Used to rehydrate the JSON body included with the POST requesting an entitlement.</remarks>
    public class RenewRequestBody
    {
        /// <summary>
        /// The requested duration in ISO-8601 format
        /// </summary>
        public string Duration { get; set; }
    }
}
