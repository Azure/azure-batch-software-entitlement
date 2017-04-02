using System;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class TokenEnforcementTests
    {
        // A valid software entitlement to use for testing
        private readonly NodeEntitlements _validEntitlements;

        // Generator used to create a token
        private readonly TokenGenerator _generator;

        // Verifier used to check the token 
        private readonly TokenVerifier _verifier;

        // Current time - captured as a member so it doesn't change during a test
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        // Key used to sign the token
        private readonly SymmetricSecurityKey _signingKey;

        // A application identifiers for testing
        private readonly string _contosoFinanceApp = "contosofinance";
        private readonly string _contosoITApp = "contosoit";
        private readonly string _contosoHRApp = "contosohr";

        // IP addresses to use 
        private readonly IPAddress _otherAddress = IPAddress.Parse("40.84.199.233");
        private readonly IPAddress _approvedAddress = IPAddress.Parse("191.239.213.197");

        // Name for the approved entitlement
        private readonly string _entitlementIdentifer = "mystery-identifier";

        public TokenEnforcementTests()
        {
            // Hard coded key for testing; actual operation will use a cert
            var plainTextSecurityKey = "This is my shared, not so secret, secret!";
            _signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSecurityKey));

            _validEntitlements = CreateEntitlements();
            _verifier = new TokenVerifier(_signingKey);
            _generator = new TokenGenerator(_signingKey, NullLogger.Instance);
        }

        private NodeEntitlements CreateEntitlements(EntitlementOptions options = EntitlementOptions.None)
        {
            var result = new NodeEntitlements()
                .WithVirtualMachineId("virtual-machine-identifier")
                .FromInstant(_now)
                .UntilInstant(_now + TimeSpan.FromDays(7));

            if (!options.HasFlag(EntitlementOptions.OmitIpAddress))
            {
                result = result.WithIpAddress(_approvedAddress);
            }

            if (!options.HasFlag(EntitlementOptions.OmitIdentifier))
            {
                result = result.WithIdentifier(_entitlementIdentifer);
            }

            if (!options.HasFlag(EntitlementOptions.OmitApplication))
            {
                result = result.AddApplication(_contosoFinanceApp);
            }

            return result;
        }

        [Flags]
        private enum EntitlementOptions
        {
            None = 0,
            OmitIpAddress = 1,
            OmitIdentifier = 2,
            OmitApplication = 4
        }

        public class ConfigurationCheck : TokenEnforcementTests
        {
            // Base case check that our valid entitlement actually works to create a token
            // If this test fails, first check to see if our test data is still valid
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
            }
        }

        public class TokenTimeSpan : TokenEnforcementTests
        {
            private readonly TimeSpan _oneWeek = TimeSpan.FromDays(7);

            private readonly TimeSpan _oneDay = TimeSpan.FromDays(1);

            [Fact]
            public void GivenValidEntitlement_HasExpectedNotBefore()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.NotBefore.Should().BeCloseTo(_validEntitlements.NotBefore, precision: 1000);
            }

            [Fact]
            public void GivenValidEntitlement_HasExpectedNotAfter()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.NotAfter.Should().BeCloseTo(_validEntitlements.NotAfter, precision: 1000);
            }

            [Fact]
            public void WhenEntitlementHasExpired_ReturnsExpectedError()
            {
                var entitlement = _validEntitlements
                    .FromInstant(_now - _oneWeek)
                    .UntilInstant(_now - _oneDay);
                var token = _generator.Generate(entitlement);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("expired"));
            }

            [Fact]
            public void WhenEntitlementHasNotYetStarted_ReturnsExpectedError()
            {
                var entitlement = _validEntitlements
                    .FromInstant(_now + _oneDay)
                    .UntilInstant(_now + _oneWeek);
                var token = _generator.Generate(entitlement);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("will not be valid"));
            }
        }

        public class VirtualMachineIdentifier : TokenEnforcementTests
        {
            [Fact]
            public void WhenIdentifierIncluded_IsReturnedByVerifier()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.VirtualMachineId.Should().Be(_validEntitlements.VirtualMachineId);
            }
        }

        public class Applications : TokenEnforcementTests
        {
            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedApplication_ReturnsExpectedApplication()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.Applications.Should().Contain(_contosoFinanceApp);
            }

            [Fact]
            public void WhenEntitlementContainsOnlyADifferentApplication_ReturnsError()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoITApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_contosoITApp));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsButNotTheRequestedApplication_ReturnsError()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoITApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().NotBeEmpty();
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsIncludingTheRequestedApplication_ReturnsExpectedApplication()
            {
                var entitlement =
                    _validEntitlements.AddApplication(_contosoHRApp)
                        .AddApplication(_contosoITApp);
                var token = _generator.Generate(entitlement);
                var result = _verifier.Verify(token, _contosoHRApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.Applications.Should().Contain(_contosoHRApp);
            }

            [Fact]
            public void WhenEntitlementContainsNoApplications_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementOptions.OmitApplication);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoITApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_contosoITApp));
            }
        }

        public class IpAddressProperty : TokenEnforcementTests
        {
            [Fact]
            public void WhenEntitlementContainsIp_ReturnsIpAddress()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.IpAddress.Should().Be(_approvedAddress);
            }

            [Fact]
            public void WhenEntitlementContainsOtherIp_ReturnsError()
            {
                var entitlements =
                    _validEntitlements.WithIpAddress(_otherAddress);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().NotBeEmpty();
            }

            [Fact]
            public void WhenEntitlementHasNoIp_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementOptions.OmitIpAddress);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_approvedAddress.ToString()));
            }
        }

        public class IdentifierProperty : TokenEnforcementTests
        {
            [Fact]
            public void WhenValidEntitlementSpecifiesIdentifier_ReturnsIdentifier()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.Identifier.Should().Be(_entitlementIdentifer);
            }

            [Fact]
            public void WhenIdentifierOmitted_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementOptions.OmitIdentifier);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("identifier"));
            }
        }
    }
}
