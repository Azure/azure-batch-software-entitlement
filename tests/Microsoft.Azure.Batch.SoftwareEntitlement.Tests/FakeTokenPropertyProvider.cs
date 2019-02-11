using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public sealed class FakeTokenPropertyProvider : ITokenPropertyProvider
    {
        /// <summary>
        /// Creates a <see cref="FakeTokenPropertyProvider"/> with non-error values for every property, corresponding
        /// to the "Default..." fields on the class.
        /// </summary>
        /// <remarks>
        /// Although every property has a value, that doesn't mean that an <see cref="EntitlementTokenProperties"/>
        /// generated from the result corresponds to a valid token. Specifically, the lifetime properties (not-before
        /// and not-after) intentionally result in an expired token, to encourage token property provision to be
        /// tested separately from token verification.
        /// </remarks>
        public static FakeTokenPropertyProvider CreateDefault()
        {
            return new FakeTokenPropertyProvider
            {
                ApplicationIds = DefaultApplicationIds.AsOk(),
                Audience = DefaultAudience.AsOk(),
                TokenId = DefaultTokenId.AsOk(),
                IpAddresses = DefaultIpAddresses.AsOk(),
                IssuedAt = DefaultIssuedAt.AsOk(),
                Issuer = DefaultIssuer.AsOk(),
                NotAfter = (DefaultNotBefore + DefaultLifetime).AsOk(),
                NotBefore = DefaultNotBefore.AsOk(),
                VirtualMachineId = DefaultVirtualMachineId.AsOk()
            };
        }

        public static readonly IEnumerable<string> DefaultApplicationIds = new[] {"contosoapp"}.AsEnumerable();

        public static readonly string DefaultAudience = "https://audience.region.batch.azure.test";

        public static readonly string DefaultTokenId = "token-fbacd5f2-0bce-46db-a374-2682c975d95d";

        public static readonly IEnumerable<IPAddress> DefaultIpAddresses =
            new List<IPAddress> {IPAddress.Parse("127.0.0.1")}.AsEnumerable();

        public static readonly string DefaultIssuer = "https://issuer.region.batch.azure.test";

        public static readonly string DefaultVirtualMachineId = "Sample";

        public static readonly DateTimeOffset DefaultIssuedAt = new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

        public static readonly DateTimeOffset DefaultNotBefore = DefaultIssuedAt;

        public static readonly DateTimeOffset DefaultNotAfter = DefaultIssuedAt + DefaultLifetime;

        public Result<IEnumerable<string>, ErrorSet> ApplicationIds { get; set; }

        public Result<string, ErrorSet> Audience { get; set; }

        public Result<string, ErrorSet> TokenId { get; set; }

        public Result<IEnumerable<IPAddress>, ErrorSet> IpAddresses { get; set; }

        public Result<DateTimeOffset, ErrorSet> IssuedAt { get; set; }

        public Result<string, ErrorSet> Issuer { get; set; }

        public Result<DateTimeOffset, ErrorSet> NotAfter { get; set; }

        public Result<DateTimeOffset, ErrorSet> NotBefore { get; set; }

        public Result<string, ErrorSet> VirtualMachineId { get; set; }

        Result<IEnumerable<string>, ErrorSet> ITokenPropertyProvider.ApplicationIds() => ApplicationIds;

        Result<string, ErrorSet> ITokenPropertyProvider.Audience() => Audience;

        Result<string, ErrorSet> ITokenPropertyProvider.TokenId() => TokenId;

        Result<IEnumerable<IPAddress>, ErrorSet> ITokenPropertyProvider.IpAddresses() => IpAddresses;

        Result<DateTimeOffset, ErrorSet> ITokenPropertyProvider.IssuedAt() => IssuedAt;

        Result<string, ErrorSet> ITokenPropertyProvider.Issuer() => Issuer;

        Result<DateTimeOffset, ErrorSet> ITokenPropertyProvider.NotAfter() => NotAfter;

        Result<DateTimeOffset, ErrorSet> ITokenPropertyProvider.NotBefore() => NotBefore;

        Result<string, ErrorSet> ITokenPropertyProvider.VirtualMachineId() => VirtualMachineId;
    }
}
