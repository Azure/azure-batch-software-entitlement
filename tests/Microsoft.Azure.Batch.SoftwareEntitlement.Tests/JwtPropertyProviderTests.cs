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
    public class JwtPropertyProviderTests
    {
        // The entitlements used to generate the token
        private readonly EntitlementTokenProperties _sourceTokenProperties;

        // Current time - captured as a member so it doesn't change during a test
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        public JwtPropertyProviderTests()
        {
            _sourceTokenProperties = EntitlementTokenProperties.Build(FakeTokenPropertyProvider.CreateValid()).AssertOk();
        }

        private static JwtPropertyProvider CreatePropertyProvider(EntitlementTokenProperties sourceTokenProperties)
        {
            var generator = new TokenGenerator(NullLogger.Instance);
            var tokenString = generator.Generate(sourceTokenProperties);
            var (principal, token) = ParseToken(tokenString);

            return new JwtPropertyProvider(principal, token);
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

        public class NotBefore : JwtPropertyProviderTests
        {
            [Fact]
            public void WhenJwtStillValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-1);
                var validTo = _now.AddDays(1);
                var tokenProperties = _sourceTokenProperties.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.NotBefore().AssertOk().Should().BeCloseTo(validFrom, precision: 1000);
            }

            [Fact]
            public void WhenJwtNotYetValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(1);
                var validTo = _now.AddDays(2);
                var tokenProperties = _sourceTokenProperties.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(tokenProperties);

                // The property provider is not responsible for validating not-before/not-after.
                // It is expected to simply return what is provided in the JWT, which is assumed
                // to have been validated when it was parsed.
                provider.NotBefore().AssertOk().Should().BeCloseTo(validFrom, precision: 1000);
            }
        }

        public class NotAfter : JwtPropertyProviderTests
        {
            [Fact]
            public void WhenJwtStillValid_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-1);
                var validTo = _now.AddDays(1);
                var tokenProperties = _sourceTokenProperties.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.NotAfter().AssertOk().Should().BeCloseTo(validTo, precision: 1000);
            }

            [Fact]
            public void WhenJwtAlreadyExpired_ReturnsSpecifiedValue()
            {
                var validFrom = _now.AddDays(-2);
                var validTo = _now.AddDays(-1);
                var tokenProperties = _sourceTokenProperties.FromInstant(validFrom).UntilInstant(validTo);
                var provider = CreatePropertyProvider(tokenProperties);

                // The property provider is not responsible for validating not-before/not-after.
                // It is expected to simply return what is provided in the JWT, which is assumed
                // to have been validated when it was parsed.
                provider.NotAfter().AssertOk().Should().BeCloseTo(validTo, precision: 1000);
            }
        }

        public class Audience : JwtPropertyProviderTests
        {
            private readonly string _audience = "http://any.audience";

            [Fact]
            public void WhenAudienceSpecified_ReturnsSpecifiedValue()
            {
                var tokenProperties = _sourceTokenProperties.WithAudience(_audience);
                var provider = CreatePropertyProvider(tokenProperties);

                // The property provider is not responsible for validating that the audience
                // matches an expected value (which is not known at this point).
                provider.Audience().AssertOk().Should().Be(_audience);
            }
        }

        public class Issuer : JwtPropertyProviderTests
        {
            private readonly string _issuer = "http://any.issuer";

            [Fact]
            public void WhenIssuerSpecified_ReturnsSpecifiedValue()
            {
                var tokenProperties = _sourceTokenProperties.WithIssuer(_issuer);
                var provider = CreatePropertyProvider(tokenProperties);

                // The property provider is not responsible for validating that the issuer
                // matches an expected value (which is not known at this point).
                provider.Issuer().AssertOk().Should().Be(_issuer);
            }
        }

        public class IssuedAt : JwtPropertyProviderTests
        {
            private readonly DateTimeOffset _inPast = new DateTime(2016, 1, 1);

            [Fact]
            public void WhenIssueDateSpecified_ReturnsSpecifiedValue()
            {
                var tokenProperties = _sourceTokenProperties.WithIssuedAt(_inPast);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.IssuedAt().AssertOk().Should().Be(_inPast);
            }
        }

        public class VirtualMachineIdentifier : JwtPropertyProviderTests
        {
            [Fact]
            public void WhenIdentifierIncluded_ReturnsSpecifiedValue()
            {
                var provider = CreatePropertyProvider(_sourceTokenProperties);
                provider.VirtualMachineId().AssertOk().Should().Be(_sourceTokenProperties.VirtualMachineId);
            }

            [Fact]
            public void WhenIdentifierOmitted_ReturnsNull()
            {
                var tokenProperties = _sourceTokenProperties.WithVirtualMachineId(null);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.VirtualMachineId().AssertOk().Should().BeNull();
            }
        }

        public class Applications : JwtPropertyProviderTests
        {
            private readonly string _app1 = "contosofinance";
            private readonly string _app2 = "contosoit";
            private readonly string _app3 = "contosohr";

            [Fact]
            public void WhenSingleApplicationSpecified_ReturnsSpecifiedValue()
            {
                var tokenProperties = _sourceTokenProperties.WithApplications(_app1);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.ApplicationIds().AssertOk().Should().Equal(_app1);
            }

            [Fact]
            public void WhenMultipleApplicationsSpecified_ReturnsAllSpecifiedValues()
            {
                var tokenProperties = _sourceTokenProperties.WithApplications(_app1, _app2, _app3);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.ApplicationIds().AssertOk().Should().BeEquivalentTo(_app1, _app2, _app3);
            }

            [Fact]
            public void WhenNoApplicationsSpecified_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties.WithApplications();
                var provider = CreatePropertyProvider(tokenProperties);
                provider.ApplicationIds().AssertError().Should().Contain("No application id claims found in token.");
            }
        }

        public class IpAddresses : JwtPropertyProviderTests
        {
            private readonly IPAddress _addr1 = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _addr2 = IPAddress.Parse("203.0.113.43");
            private readonly IPAddress _addr3 = IPAddress.Parse("203.0.113.44");

            [Fact]
            public void WhenSingleIpAddressSpecified_ReturnsSpecifiedValue()
            {
                var tokenProperties = _sourceTokenProperties.WithIpAddresses(_addr1);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.IpAddresses().AssertOk().Should().Equal(_addr1);
            }

            [Fact]
            public void WhenMultipleIpAddressesSpecified_ReturnsAllSpecifiedValues()
            {
                var tokenProperties = _sourceTokenProperties.WithIpAddresses(_addr1, _addr2, _addr3);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.IpAddresses().AssertOk().Should().BeEquivalentTo(_addr1, _addr2, _addr3);
            }

            [Fact]
            public void WhenNoIpAddressesSpecified_ReturnsNoValues()
            {
                var tokenProperties = _sourceTokenProperties.WithIpAddresses();
                var provider = CreatePropertyProvider(tokenProperties);
                provider.IpAddresses().AssertOk().Should().BeEmpty();
            }
        }

        public class Identifier : JwtPropertyProviderTests
        {
            [Fact]
            public void WhenIdentifierSpecified_ReturnsSpecifiedValue()
            {
                var provider = CreatePropertyProvider(_sourceTokenProperties);
                provider.TokenId().AssertOk().Should().Be(_sourceTokenProperties.Identifier);
            }

            [Fact]
            public void WhenIdentifierOmitted_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties.WithIdentifier(null);
                var provider = CreatePropertyProvider(tokenProperties);
                provider.TokenId().AssertError().Should().Contain(e => e.Contains("identifier"));
            }
        }
    }
}
