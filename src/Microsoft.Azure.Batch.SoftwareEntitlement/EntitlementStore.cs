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

        public Errorable<EntitlementProperties> FindEntitlement(string entitlementId)
        {
            if (!_entitlements.TryGetValue(entitlementId, out var entitlementProperties))
            {
                return Errorable.Failure<EntitlementProperties>($"Entitlement {entitlementId} not found.");
            }

            return Errorable.Success(entitlementProperties);
        }

        public Errorable<EntitlementProperties> RenewEntitlement(
            string entitlementId,
            DateTime renewalTime) =>
            TryUpdate(
                entitlementId,
                props => props.WithRenewal(renewalTime),
                $"Unable to store renewal event for {entitlementId}");

        public Errorable<EntitlementProperties> ReleaseEntitlement(
            string entitlementId,
            DateTime releaseTime) =>
            TryUpdate(
                entitlementId,
                props => props.WithRelease(releaseTime),
                $"Unable to store release event for {entitlementId}");

        private Errorable<EntitlementProperties> TryUpdate(
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
                    return Errorable.Success(updatedEntitlementProperties);
                }
            }

            return Errorable.Failure<EntitlementProperties>(errorMessage);
        }
    }
}
