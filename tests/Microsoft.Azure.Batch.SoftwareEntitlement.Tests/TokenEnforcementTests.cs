using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
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

        public TokenEnforcementTests()
        {
            // Hard coded key for testing; actual operation will use a cert
            var plainTextSecurityKey = "This is my shared, not so secret, secret!";
            _signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSecurityKey));

            _validEntitlements = new NodeEntitlements()
                .WithVirtualMachineId("virtual-machine-identifier")
                .FromInstant(_now)
                .UntilInstant(_now + TimeSpan.FromDays(7));

            _verifier = new TokenVerifier(_signingKey);
            _generator = new TokenGenerator(_signingKey);
        }

        public class ConfigurationCheck : TokenEnforcementTests
        {
            // Base case check that our valid entitlement actually works to create a token
            // If this test fails, first check to see if our test data is still valid
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token);
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
                var result = _verifier.Verify(token);
                result.Value.NotBefore.Should().BeCloseTo(_validEntitlements.NotBefore, precision: 1000);
            }

            [Fact]
            public void GivenValidEntitlement_HasExpectedNotAfter()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token);
                result.Value.NotAfter.Should().BeCloseTo(_validEntitlements.NotAfter, precision: 1000);
            }

            [Fact]
            public void WhenEntitlementHasExpired_ReturnsExpectedError()
            {
                var entitlement = _validEntitlements
                    .FromInstant(_now - _oneWeek)
                    .UntilInstant(_now - _oneDay);
                var token = _generator.Generate(entitlement);
                var result = _verifier.Verify(token);
                result.Errors.Should().Contain(e => e.Contains("expired"));
            }

            [Fact]
            public void WhenEntitlementHasNotYetStarted_ReturnsExpectedError()
            {
                var entitlement = _validEntitlements
                    .FromInstant(_now + _oneDay)
                    .UntilInstant(_now + _oneWeek);
                var token = _generator.Generate(entitlement);
                var result = _verifier.Verify(token);
                result.Errors.Should().Contain(e => e.Contains("will not be valid"));
            }
        }

        public class VirtualMachineIdentifier : TokenEnforcementTests
        {
            [Fact]
            public void ValueEmbeddedInToken_IsReturnedByVerifier()
            {
                var token = _generator.Generate(_validEntitlements);
                var result = _verifier.Verify(token);
                result.Value.VirtualMachineId.Should().Be(_validEntitlements.VirtualMachineId);
            }
        }
    }
}
