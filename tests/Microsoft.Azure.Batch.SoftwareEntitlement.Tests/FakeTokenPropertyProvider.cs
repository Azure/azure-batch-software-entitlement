using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public sealed class FakeTokenPropertyProvider : ITokenPropertyProvider
    {
        public static FakeTokenPropertyProvider CreateValid(DateTimeOffset? issuedAt = null)
        {
            issuedAt = issuedAt ?? DateTimeOffset.Now;
            return new FakeTokenPropertyProvider
            {
                ApplicationIds = Result.FromOk(DefaultApplicationIds),
                Audience = Result.FromOk(DefaultAudience),
                TokenId = Result.FromOk(DefaultTokenId),
                IpAddresses = Result.FromOk(DefaultIpAddresses),
                IssuedAt = Result.FromOk(issuedAt.Value),
                Issuer = Result.FromOk(DefaultIssuer),
                NotAfter = Result.FromOk(issuedAt.Value + DefaultLifetime),
                NotBefore = Result.FromOk(issuedAt.Value),
                VirtualMachineId = Result.FromOk(DefaultVirtualMachineId)
            };
        }

        public static readonly IEnumerable<string> DefaultApplicationIds = new[] {"contosoapp"}.AsEnumerable();

        public static readonly string DefaultAudience = "https://audience.region.batch.azure.test";

        public static readonly string DefaultTokenId = "token-fbacd5f2-0bce-46db-a374-2682c975d95d";

        public static readonly IEnumerable<IPAddress> DefaultIpAddresses =
            new List<IPAddress> {IPAddress.Parse("127.0.0.1")}.AsEnumerable();

        public static readonly string DefaultIssuer = "https://issuer.region.batch.azure.test";

        public static readonly string DefaultVirtualMachineId = "Sample";

        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

        public Result<IEnumerable<string>, ErrorCollection> ApplicationIds { get; set; }

        public Result<string, ErrorCollection> Audience { get; set; }

        public Result<string, ErrorCollection> TokenId { get; set; }

        public Result<IEnumerable<IPAddress>, ErrorCollection> IpAddresses { get; set; }

        public Result<DateTimeOffset, ErrorCollection> IssuedAt { get; set; }

        public Result<string, ErrorCollection> Issuer { get; set; }

        public Result<DateTimeOffset, ErrorCollection> NotAfter { get; set; }

        public Result<DateTimeOffset, ErrorCollection> NotBefore { get; set; }

        public Result<string, ErrorCollection> VirtualMachineId { get; set; }

        Result<IEnumerable<string>, ErrorCollection> ITokenPropertyProvider.ApplicationIds() => ApplicationIds;

        Result<string, ErrorCollection> ITokenPropertyProvider.Audience() => Audience;

        Result<string, ErrorCollection> ITokenPropertyProvider.TokenId() => TokenId;

        Result<IEnumerable<IPAddress>, ErrorCollection> ITokenPropertyProvider.IpAddresses() => IpAddresses;

        Result<DateTimeOffset, ErrorCollection> ITokenPropertyProvider.IssuedAt() => IssuedAt;

        Result<string, ErrorCollection> ITokenPropertyProvider.Issuer() => Issuer;

        Result<DateTimeOffset, ErrorCollection> ITokenPropertyProvider.NotAfter() => NotAfter;

        Result<DateTimeOffset, ErrorCollection> ITokenPropertyProvider.NotBefore() => NotBefore;

        Result<string, ErrorCollection> ITokenPropertyProvider.VirtualMachineId() => VirtualMachineId;
    }
}
