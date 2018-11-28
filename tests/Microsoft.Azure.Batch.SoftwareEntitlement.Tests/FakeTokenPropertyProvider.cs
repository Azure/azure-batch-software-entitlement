using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public sealed class FakeTokenPropertyProvider : ITokenPropertyProvider
    {
        public static FakeTokenPropertyProvider CreateValid()
        {
            var now = DateTimeOffset.Now;
            return new FakeTokenPropertyProvider
            {
                ApplicationIds = Errorable.Success(new[] { "contosoapp" }.AsEnumerable()),
                Audience = Errorable.Success("https://audience.region.batch.azure.test"),
                TokenId = Errorable.Success("entitlement-fbacd5f2-0bce-46db-a374-2682c975d95d"),
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

        public Errorable<string> TokenId { get; set; }

        public Errorable<IEnumerable<IPAddress>> IpAddresses { get; set; }

        public Errorable<DateTimeOffset> IssuedAt { get; set; }

        public Errorable<string> Issuer { get; set; }

        public Errorable<DateTimeOffset> NotAfter { get; set; }

        public Errorable<DateTimeOffset> NotBefore { get; set; }

        public Errorable<string> VirtualMachineId { get; set; }

        Errorable<IEnumerable<string>> ITokenPropertyProvider.ApplicationIds() => ApplicationIds;

        Errorable<string> ITokenPropertyProvider.Audience() => Audience;

        Errorable<string> ITokenPropertyProvider.TokenId() => TokenId;

        Errorable<IEnumerable<IPAddress>> ITokenPropertyProvider.IpAddresses() => IpAddresses;

        Errorable<DateTimeOffset> ITokenPropertyProvider.IssuedAt() => IssuedAt;

        Errorable<string> ITokenPropertyProvider.Issuer() => Issuer;

        Errorable<DateTimeOffset> ITokenPropertyProvider.NotAfter() => NotAfter;

        Errorable<DateTimeOffset> ITokenPropertyProvider.NotBefore() => NotBefore;

        Errorable<string> ITokenPropertyProvider.VirtualMachineId() => VirtualMachineId;
    }
}
