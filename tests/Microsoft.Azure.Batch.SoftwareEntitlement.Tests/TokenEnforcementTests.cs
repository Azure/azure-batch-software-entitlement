using System;
using System.Net;
using System.Text;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

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

        // A application identifiers for testing
        private readonly string _contosoFinanceApp = "contosofinance";
        private readonly string _contosoITApp = "contosoit";
        private readonly string _contosoHRApp = "contosohr";

        // IP addresses to use
        private readonly IPAddress _otherAddress = IPAddress.Parse("203.0.113.42");
        private readonly IPAddress _approvedAddress = IPAddress.Parse("203.0.113.45");

        // Name for the approved entitlement
        private readonly string _entitlementIdentifer = "mystery-identifier";

        // Logger that does nothing
        private readonly ILogger _nullLogger = NullLogger.Instance;

        // Key to use for signing
        private readonly SymmetricSecurityKey _signingKey;

        // Key to use for encryption
        private readonly SymmetricSecurityKey _encryptingKey;

        // Credentials used for encryption
        private readonly EncryptingCredentials _encryptingCredentials;

        // Credentials used for signing
        private readonly SigningCredentials _signingCredentials;

        public TokenEnforcementTests()
        {
            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextSigningKey = "This is my shared, not so secret, secret that needs to be very long!";
            _signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSigningKey));

            _signingCredentials = new SigningCredentials(
                _signingKey, SecurityAlgorithms.HmacSha256Signature);

            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextEncryptionKey = "This is another not so secret, secret that needs to be very long!";
            _encryptingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextEncryptionKey));

            _encryptingCredentials = new EncryptingCredentials(
                _encryptingKey, "dir", SecurityAlgorithms.Aes256CbcHmacSha512);

            _validEntitlements = CreateEntitlements();
            _verifier = new TokenVerifier(_signingKey, _encryptingKey);
            _generator = new TokenGenerator(_nullLogger, _signingCredentials, _encryptingCredentials);
        }

        private NodeEntitlements CreateEntitlements(EntitlementCreationOptions creationOptions = EntitlementCreationOptions.None)
        {
            var result = new NodeEntitlements()
                .FromInstant(_now)
                .UntilInstant(_now + TimeSpan.FromDays(7));

            if (!creationOptions.HasFlag(EntitlementCreationOptions.OmitIpAddress))
            {
                result = result.AddIpAddress(_approvedAddress);
            }

            if (!creationOptions.HasFlag(EntitlementCreationOptions.OmitIdentifier))
            {
                result = result.WithIdentifier(_entitlementIdentifer);
            }

            if (!creationOptions.HasFlag(EntitlementCreationOptions.OmitApplication))
            {
                result = result.AddApplication(_contosoFinanceApp);
            }

            if (!creationOptions.HasFlag(EntitlementCreationOptions.OmitMachineId))
            {
                result = result.WithVirtualMachineId("virtual-machine-identifier");
            }

            return result;
        }

        /// <summary>
        /// Options used to control the creation of an <see cref="NodeEntitlements"/> instance
        /// for testing.
        /// </summary>
        [Flags]
        private enum EntitlementCreationOptions
        {
            None = 0,
            OmitIpAddress = 1,
            OmitIdentifier = 2,
            OmitApplication = 4,
            OmitMachineId = 8
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

            [Fact]
            public void WhenIdentifierOmitted_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementCreationOptions.OmitMachineId);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("machine identifier"));
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
                var entitlements = CreateEntitlements(EntitlementCreationOptions.OmitApplication);
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
                result.Value.IpAddresses.Should().Contain(_approvedAddress);
            }

            [Fact]
            public void WhenEntitlementContainsOtherIp_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementCreationOptions.OmitIpAddress)
                    .AddIpAddress(_otherAddress);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().NotBeEmpty();
            }

            [Fact]
            public void WhenEntitlementHasNoIp_ReturnsError()
            {
                var entitlements = CreateEntitlements(EntitlementCreationOptions.OmitIpAddress);
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
                var entitlements = CreateEntitlements(EntitlementCreationOptions.OmitIdentifier);
                var token = _generator.Generate(entitlements);
                var result = _verifier.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("identifier"));
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no signing key 
        /// </summary>
        public class WithoutSigning : TokenEnforcementTests
        {
            // Generator with no signing key used to create a token
            private readonly TokenGenerator _generatorWithNoSigningKey;

            // Verifier with no signing key used to check the token
            private readonly TokenVerifier _verifierWithNoSigningKey;

            public WithoutSigning()
            {
                _verifierWithNoSigningKey = new TokenVerifier(encryptingKey: _encryptingKey);
                _generatorWithNoSigningKey = new TokenGenerator(_nullLogger, null, _encryptingCredentials);
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedApplication_ReturnsExpectedApplication()
            {
                var token = _generatorWithNoSigningKey.Generate(_validEntitlements);
                var result = _verifierWithNoSigningKey.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.Applications.Should().Contain(_contosoFinanceApp);
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no encryption key 
        /// </summary>
        public class WithoutEncryption : TokenEnforcementTests
        {
            // Generator with no signing key used to create a token
            private readonly TokenGenerator _generatorWithNoEncryptionKey;

            // Verifier with no signing key used to check the token
            private readonly TokenVerifier _verifierWithNoEncryptionKey;

            public WithoutEncryption()
            {
                _verifierWithNoEncryptionKey = new TokenVerifier(signingKey: _signingKey);
                _generatorWithNoEncryptionKey = new TokenGenerator(_nullLogger, _signingCredentials, encryptingCredentials: null);
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedApplication_ReturnsExpectedApplication()
            {
                var token = _generatorWithNoEncryptionKey.Generate(_validEntitlements);
                var result = _verifierWithNoEncryptionKey.Verify(token, _contosoFinanceApp, _approvedAddress);
                result.HasValue.Should().BeTrue();
                result.Value.Applications.Should().Contain(_contosoFinanceApp);
            }
        }
    }
}
