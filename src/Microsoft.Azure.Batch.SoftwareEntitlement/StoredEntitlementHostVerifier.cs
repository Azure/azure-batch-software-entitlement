using System.Collections.Concurrent;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// An in-memory store of entitlement IDs and the corresponding host ID values
    /// with which they have been used. For ensuring that every entitlement is only
    /// used by a single host.
    /// </summary>
    public class StoredEntitlementHostVerifier : IHostVerifier
    {
        private readonly ConcurrentDictionary<string, string> _lookup = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Performs a check to verify that the entitlement was not previously
        /// associated with a different host.
        /// </summary>
        /// <param name="entitlement">The entitlement ID</param>
        /// <param name="hostId">The host ID</param>
        /// <returns>
        /// True if the entitlement is new or previously associated with the same host,
        /// false if it was previously associated with a different host.
        /// </returns>
        public bool Verify(NodeEntitlements entitlement, string hostId)
        {
            var storedHostId = _lookup.GetOrAdd(entitlement.Identifier, hostId);
            return storedHostId == hostId;
        }
    }
}
