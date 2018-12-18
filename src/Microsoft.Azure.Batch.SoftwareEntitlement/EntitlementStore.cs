using System.Collections.Concurrent;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class EntitlementStore
    {
        private readonly ConcurrentDictionary<string, EntitlementStatus> _entitlements =
            new ConcurrentDictionary<string, EntitlementStatus>();

        public void StoreEntitlementId(string entitlementId) =>
            _entitlements.AddOrUpdate(entitlementId, EntitlementStatus.Acquired, (e, s) => s);

        public bool ContainsEntitlementId(string entitlementId) =>
            _entitlements.ContainsKey(entitlementId);

        public void RenewEntitlement(string entitlementId) { }

        public void ReleaseEntitlement(string entitlementId) =>
            _entitlements.TryUpdate(entitlementId, EntitlementStatus.Released, EntitlementStatus.Acquired);

        public bool IsReleased(string entitlementId) =>
            _entitlements.TryGetValue(entitlementId, out var status) && status == EntitlementStatus.Released;

        private enum EntitlementStatus
        {
            Acquired,
            Released
        }
    }
}
