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
                VirtualMachineId = Errorable.Success("Sample"),
                CpuCoreCount = Errorable.Success(4),
                BatchAccountId = Errorable.Success((string)null),
                PoolId = Errorable.Success((string)null),
                JobId = Errorable.Success((string)null),
                TaskId = Errorable.Success((string)null)
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

        public Errorable<int> CpuCoreCount { get; set; }

        public Errorable<string> BatchAccountId { get; set; }

        public Errorable<string> PoolId { get; set; }

        public Errorable<string> JobId { get; set; }

        public Errorable<string> TaskId { get; set; }

        Errorable<IEnumerable<string>> IEntitlementPropertyProvider.ApplicationIds() => ApplicationIds;

        Errorable<string> IEntitlementPropertyProvider.Audience() => Audience;

        Errorable<string> IEntitlementPropertyProvider.EntitlementId() => EntitlementId;

        Errorable<IEnumerable<IPAddress>> IEntitlementPropertyProvider.IpAddresses() => IpAddresses;

        Errorable<int> IEntitlementPropertyProvider.CpuCoreCount() => CpuCoreCount;

        Errorable<string> IEntitlementPropertyProvider.BatchAccountId() => BatchAccountId;

        Errorable<string> IEntitlementPropertyProvider.PoolId() => PoolId;

        Errorable<string> IEntitlementPropertyProvider.JobId() => JobId;

        Errorable<string> IEntitlementPropertyProvider.TaskId() => TaskId;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.IssuedAt() => IssuedAt;

        Errorable<string> IEntitlementPropertyProvider.Issuer() => Issuer;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.NotAfter() => NotAfter;

        Errorable<DateTimeOffset> IEntitlementPropertyProvider.NotBefore() => NotBefore;

        Errorable<string> IEntitlementPropertyProvider.VirtualMachineId() => VirtualMachineId;
    }
}
