using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public sealed class FakeEntitlementPropertyProvider : IEntitlementPropertyProvider
    {
        public static FakeEntitlementPropertyProvider CreateValid()
        {
            var now = DateTimeOffset.Now;
            return new FakeEntitlementPropertyProvider
            {
                ApplicationIds = Errorable.Success(new[] { "contosoapp" }.AsEnumerable()),
                Audience = Errorable.Success("https://audience.region.batch.azure.test"),
                EntitlementId = Errorable.Success("entitlement-fbacd5f2-0bce-46db-a374-2682c975d95d"),
                IpAddresses = Errorable.Success(new List<IPAddress> { IPAddress.Parse("127.0.0.1") }.AsEnumerable()),
                IssuedAt = Errorable.Success(now),
                Issuer = Errorable.Success("https://issuer.region.batch.azure.test"),
                NotAfter = Errorable.Success(now + TimeSpan.FromDays(7)),
                NotBefore = Errorable.Success(now),
                VirtualMachineId = Errorable.Success("Sample")
            };
        }

        public Errorable<IEnumerable<string>> ApplicationIds { get; set; }

        public Errorable<string> Audience { get; set; }

        public Errorable<string> EntitlementId { get; set; }

        public Errorable<IEnumerable<IPAddress>> IpAddresses { get; set; }

        public Errorable<DateTimeOffset> IssuedAt { get; set; }

        public Errorable<string> Issuer { get; set; }

        public Errorable<DateTimeOffset> NotAfter { get; set; }

        public Errorable<DateTimeOffset> NotBefore { get; set; }

        public Errorable<string> VirtualMachineId { get; set; }

        Errorable<IEnumerable<string>> IEntitlementPropertyProvider.ApplicationIds() => ApplicationIds;

        Errorable<string> IEntitlementPropertyProvider.Audience() => Audience;

        Errorable<string> IEntitlementPropertyProvider.EntitlementId() => EntitlementId;

        Errorable<IEnumerable<IPAddress>> IEntitlementPropertyProvider.IpAddresses() => IpAddresses;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.IssuedAt() => IssuedAt;

        Errorable<string> IEntitlementPropertyProvider.Issuer() => Issuer;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.NotAfter() => NotAfter;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.NotBefore() => NotBefore;

        Errorable<string> IEntitlementPropertyProvider.VirtualMachineId() => VirtualMachineId;
    }
}
