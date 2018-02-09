using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class JwtEntitlementPropertyProviderTests
    {
        // The entitlements used to generate the token
        private readonly NodeEntitlements _sourceEntitlements;

        // Current time - captured as a member so it doesn't change during a test
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        public JwtEntitlementPropertyProviderTests()
        {
            _sourceEntitlements = NodeEntitlements.Build(FakeEntitlementPropertyProvider.CreateValid()).Value;
        }

        private static JwtEntitlementPropertyProvider CreatePropertyProvider(NodeEntitlements sourceEntitlements)
        {
            var generator = new TokenGenerator(NullLogger.Instance);
            var tokenString = generator.Generate(sourceEntitlements);
            var (principal, token) = ParseToken(tokenString);

            return new JwtEntitlementPropertyProvider(principal, token);
        }

        private static (ClaimsPrincipal Principal, JwtSecurityToken Token) ParseToken(string tokenString)
        {
            // We're not testing the correctness of the token here, so no validation required.
            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                RequireExpirationTime = false,
                RequireSignedTokens = false,
                ValidateIssuerSigningKey = false
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(tokenString, validationParameters, out var token);

            if (!(token is JwtSecurityToken jwt))
            {
                throw new InvalidOperationException("Token is expected to be a JWT");
            }

            return (principal, jwt);
        }

        public class NotBefore : JwtEntitlementPropertyProviderTests
        {
            [Fact]
            public void WhenJwtStillValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-1);
                var validTo = _now.AddDays(1);
                var entitlements = _sourceEntitlements.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(entitlements);
                provider.NotBefore().HasValue.Should().BeTrue();
                provider.NotBefore().Value.Should().BeCloseTo(validFrom, precision: 1000);
            }

            [Fact]
            public void WhenJwtNotYetValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(1);
                var validTo = _now.AddDays(2);
                var entitlements = _sourceEntitlements.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(entitlements);

                // The property provider is not responsible for validating not-before/not-after.
                // It is expected to simply return what is provided in the JWT, which is assumed
                // to have been validated when it was parsed.
                provider.NotBefore().HasValue.Should().BeTrue();
                provider.NotBefore().Value.Should().BeCloseTo(validFrom, precision: 1000);
            }
        }

        public class NotAfter : JwtEntitlementPropertyProviderTests
        {
            [Fact]
            public void WhenJwtStillValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-1);
                var validTo = _now.AddDays(1);
                var entitlements = _sourceEntitlements.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(entitlements);
                provider.NotAfter().HasValue.Should().BeTrue();
                provider.NotAfter().Value.Should().BeCloseTo(validTo, precision: 1000);
            }

            [Fact]
            public void WhenJwtAlreadyExpired_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-2);
                var validTo = _now.AddDays(-1);
                var entitlements = _sourceEntitlements.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(entitlements);

                // The property provider is not responsible for validating not-before/not-after.
                // It is expected to simply return what is provided in the JWT, which is assumed
                // to have been validated when it was parsed.
                provider.NotAfter().HasValue.Should().BeTrue();
                provider.NotAfter().Value.Should().BeCloseTo(validTo, precision: 1000);
            }
        }

        public class Audience : JwtEntitlementPropertyProviderTests
        {
            private readonly string _audience = "http://any.audience";

            [Fact]
            public void WhenAudienceSpecified_ReturnsSpecifiedValue()
            {
                var entitlements = _sourceEntitlements.WithAudience(_audience);
                var provider = CreatePropertyProvider(entitlements);

                // The property provider is not responsible for validating that the audience
                // matches an expected value (which is not known at this point).
                provider.Audience().HasValue.Should().BeTrue();
                provider.Audience().Value.Should().Be(_audience);
            }
        }

        public class Issuer : JwtEntitlementPropertyProviderTests
        {
            private readonly string _issuer = "http://any.issuer";

            [Fact]
            public void WhenIssuerSpecified_ReturnsSpecifiedValue()
            {
                var entitlements = _sourceEntitlements.WithIssuer(_issuer);
                var provider = CreatePropertyProvider(entitlements);

                // The property provider is not responsible for validating that the issuer
                // matches an expected value (which is not known at this point).
                provider.Issuer().HasValue.Should().BeTrue();
                provider.Issuer().Value.Should().Be(_issuer);
            }
        }

        public class IssuedAt : JwtEntitlementPropertyProviderTests
        {
            private readonly DateTimeOffset _inPast = new DateTime(2016, 1, 1);

            [Fact]
            public void WhenIssueDateSpecified_ReturnsSpecifiedValue()
            {
                var entitlements = _sourceEntitlements.WithIssuedAt(_inPast);
                var provider = CreatePropertyProvider(entitlements);
                provider.IssuedAt().HasValue.Should().BeTrue();
                provider.IssuedAt().Value.Should().Be(_inPast);
            }
        }

        public class VirtualMachineIdentifier : JwtEntitlementPropertyProviderTests
        {
            [Fact]
            public void WhenIdentifierIncluded_ReturnsSpecifiedValue()
            {
                var provider = CreatePropertyProvider(_sourceEntitlements);
                provider.VirtualMachineId().HasValue.Should().BeTrue();
                provider.VirtualMachineId().Value.Should().Be(_sourceEntitlements.VirtualMachineId);
            }

            [Fact]
            public void WhenIdentifierOmitted_ReturnsNull()
            {
                var entitlements = _sourceEntitlements.WithVirtualMachineId(null);
                var provider = CreatePropertyProvider(entitlements);
                provider.VirtualMachineId().HasValue.Should().BeTrue();
                provider.VirtualMachineId().Value.Should().BeNull();
            }
        }

        public class Applications : JwtEntitlementPropertyProviderTests
        {
            private readonly string _app1 = "contosofinance";
            private readonly string _app2 = "contosoit";
            private readonly string _app3 = "contosohr";

            [Fact]
            public void WhenSingleApplicationSpecified_ReturnsSpecifiedValue()
            {
                var entitlements = _sourceEntitlements.WithApplications(_app1);
                var provider = CreatePropertyProvider(entitlements);
                provider.ApplicationIds().HasValue.Should().BeTrue();
                provider.ApplicationIds().Value.Single().Should().Be(_app1);
            }

            [Fact]
            public void WhenMultipleApplicationsSpecified_ReturnsAllSpecifiedValues()
            {
                var entitlements = _sourceEntitlements.WithApplications(_app1, _app2, _app3);
                var provider = CreatePropertyProvider(entitlements);
                provider.ApplicationIds().HasValue.Should().BeTrue();
                provider.ApplicationIds().Value.Should().BeEquivalentTo(new[] { _app1, _app2, _app3 });
            }

            [Fact]
            public void WhenNoApplicationsSpecified_ReturnsError()
            {
                var entitlements = _sourceEntitlements.WithApplications();
                var provider = CreatePropertyProvider(entitlements);
                provider.ApplicationIds().HasValue.Should().BeFalse();
                provider.ApplicationIds().Errors.Should().Contain("No application id claims found in token.");
            }
        }

        public class IpAddresses : JwtEntitlementPropertyProviderTests
        {
            private readonly IPAddress _addr1 = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _addr2 = IPAddress.Parse("203.0.113.43");
            private readonly IPAddress _addr3 = IPAddress.Parse("203.0.113.44");

            [Fact]
            public void WhenSingleIpAddressSpecified_ReturnsSpecifiedValue()
            {
                var entitlements = _sourceEntitlements.WithIpAddresses(_addr1);
                var provider = CreatePropertyProvider(entitlements);
                provider.IpAddresses().HasValue.Should().BeTrue();
                provider.IpAddresses().Value.Single().Should().Be(_addr1);
            }

            [Fact]
            public void WhenMultipleIpAddressesSpecified_ReturnsAllSpecifiedValues()
            {
                var entitlements = _sourceEntitlements.WithIpAddresses(_addr1, _addr2, _addr3);
                var provider = CreatePropertyProvider(entitlements);
                provider.IpAddresses().HasValue.Should().BeTrue();
                provider.IpAddresses().Value.Should().BeEquivalentTo(new[] { _addr1, _addr2, _addr3 });
            }

            [Fact]
            public void WhenNoIpAddressesSpecified_ReturnsNoValues()
            {
                var entitlements = _sourceEntitlements.WithIpAddresses();
                var provider = CreatePropertyProvider(entitlements);
                provider.IpAddresses().HasValue.Should().BeTrue();
                provider.IpAddresses().Value.Should().BeEmpty();
            }
        }

        public class Identifier : JwtEntitlementPropertyProviderTests
        {
            [Fact]
            public void WhenIdentifierSpecified_ReturnsSpecifiedValue()
            {
                var provider = CreatePropertyProvider(_sourceEntitlements);
                provider.EntitlementId().HasValue.Should().BeTrue();
                provider.EntitlementId().Value.Should().Be(_sourceEntitlements.Identifier);
            }

            [Fact]
            public void WhenIdentifierOmitted_ReturnsError()
            {
                var entitlements = _sourceEntitlements.WithIdentifier(null);
                var provider = CreatePropertyProvider(entitlements);
                provider.EntitlementId().HasValue.Should().BeFalse();
                provider.EntitlementId().Errors.Should().Contain(e => e.Contains("identifier"));
            }
        }
    }
}
