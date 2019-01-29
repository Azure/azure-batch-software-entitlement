using System;
using System.Collections.Concurrent;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class EntitlementStore
    {
        private readonly ConcurrentDictionary<string, EntitlementProperties> _entitlements =
            new ConcurrentDictionary<string, EntitlementProperties>();

        public EntitlementProperties StoreEntitlement(
            string entitlementId,
            EntitlementTokenProperties tokenProperties,
            DateTime acquisitionTime) =>
            _entitlements.AddOrUpdate(
                entitlementId,
                EntitlementProperties.CreateNew(entitlementId, tokenProperties, acquisitionTime),
                (e, props) => props);

        public Result<EntitlementProperties, ErrorCollection> FindEntitlement(string entitlementId)
        {
            if (!_entitlements.TryGetValue(entitlementId, out var entitlementProperties))
            {
                return ErrorCollection.Create($"Entitlement {entitlementId} not found.");
            }

            return entitlementProperties;
        }

        public Result<EntitlementProperties, ErrorCollection> RenewEntitlement(
            string entitlementId,
            DateTime renewalTime) =>
            TryUpdate(
                entitlementId,
                props => props.WithRenewal(renewalTime),
                $"Unable to store renewal event for {entitlementId}");

        public Result<EntitlementProperties, ErrorCollection> ReleaseEntitlement(
            string entitlementId,
            DateTime releaseTime) =>
            TryUpdate(
                entitlementId,
                props => props.WithRelease(releaseTime),
                $"Unable to store release event for {entitlementId}");

        private Result<EntitlementProperties, ErrorCollection> TryUpdate(
            string entitlementId,
            Func<EntitlementProperties, EntitlementProperties> updater,
            string errorMessage)
        {
            if (_entitlements.TryGetValue(entitlementId, out var currentEntitlementProperties))
            {
                var updatedEntitlementProperties = updater(currentEntitlementProperties);
                if (_entitlements.TryUpdate(
                    entitlementId,
                    updatedEntitlementProperties,
                    currentEntitlementProperties))
                {
                    return updatedEntitlementProperties;
                }
            }

            return ErrorCollection.Create(errorMessage);
        }
    }
}
