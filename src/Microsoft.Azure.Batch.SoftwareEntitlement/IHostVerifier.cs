namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// A type which can verify whether a given hostId value is valid, given a <see cref="NodeEntitlements"/>
    /// instance.
    /// </summary>
    public interface IHostVerifier
    {
        /// <summary>
        /// Returns a value indicating whether the specified <paramref name="hostId"/> value
        /// is valid for the <paramref name="entitlement"/>.
        /// </summary>
        /// <param name="entitlement">A set of entitlements</param>
        /// <param name="hostId">An identifier for a host</param>
        /// <returns>
        /// True if the <paramref name="hostId"/> is valid, or false otherwise.
        /// </returns>
        bool Verify(NodeEntitlements entitlement, string hostId);
    }
}
