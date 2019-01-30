using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class EntitlementProperties
    {
        private EntitlementProperties(
            string entitlementId,
            EntitlementTokenProperties tokenProperties,
            DateTimeOffset acquisitionEventTime,
            IEnumerable<DateTimeOffset> renewalEventTimes,
            DateTimeOffset? releaseEventTime)
        {
            EntitlementId = entitlementId;
            TokenProperties = tokenProperties;
            AcquisitionEventTime = acquisitionEventTime;
            RenewalEventTimes = renewalEventTimes;
            ReleaseEventTime = releaseEventTime;
        }

        public static EntitlementProperties CreateNew(
            string entitlementId,
            EntitlementTokenProperties tokenProperties,
            DateTimeOffset acquisitionTime) =>
            new EntitlementProperties(
                entitlementId,
                tokenProperties,
                acquisitionTime,
                Enumerable.Empty<DateTimeOffset>(),
                null);

        public EntitlementProperties WithRenewal(DateTimeOffset renewalTime) =>
            new EntitlementProperties(
                EntitlementId,
                TokenProperties,
                AcquisitionEventTime,
                RenewalEventTimes.Append(renewalTime),
                ReleaseEventTime);

        public EntitlementProperties WithRelease(DateTimeOffset releaseTime) =>
            new EntitlementProperties(
                EntitlementId,
                TokenProperties,
                AcquisitionEventTime,
                RenewalEventTimes,
                releaseTime);

        public string EntitlementId { get; }

        public EntitlementTokenProperties TokenProperties { get; }

        public DateTimeOffset AcquisitionEventTime { get; }

        public IEnumerable<DateTimeOffset> RenewalEventTimes { get; }

        public DateTimeOffset? ReleaseEventTime { get; }

        public bool IsReleased => ReleaseEventTime.HasValue;
    }
}
