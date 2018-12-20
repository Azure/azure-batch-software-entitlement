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
            DateTime acquisitionEventTime,
            IEnumerable<DateTime> renewalEventTimes,
            DateTime? releaseEventTime)
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
            DateTime acquisitionTime) =>
            new EntitlementProperties(
                entitlementId,
                tokenProperties,
                acquisitionTime,
                Enumerable.Empty<DateTime>(),
                null);

        public EntitlementProperties WithRenewal(DateTime renewalTime) =>
            new EntitlementProperties(
                EntitlementId,
                TokenProperties,
                AcquisitionEventTime,
                RenewalEventTimes.Append(renewalTime),
                ReleaseEventTime);

        public EntitlementProperties WithRelease(DateTime releaseTime) =>
            new EntitlementProperties(
                EntitlementId,
                TokenProperties,
                AcquisitionEventTime,
                RenewalEventTimes,
                releaseTime);

        public string EntitlementId { get; }

        public EntitlementTokenProperties TokenProperties { get; }

        public DateTime AcquisitionEventTime { get; }

        public IEnumerable<DateTime> RenewalEventTimes { get; }

        public DateTime? ReleaseEventTime { get; }

        public bool IsReleased => ReleaseEventTime.HasValue;
    }
}
