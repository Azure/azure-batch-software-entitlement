using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class TokenVerifierTests
    {
        // A verifier to use as a base for testing
        private readonly TokenVerifier _verifier;

        // A valid virtual machine identifier
        private readonly string _virtualMachineId = "virtual-machine-identifier";

        // Key used to verify token signatures
        private readonly SymmetricSecurityKey _signingKey;

        // Key used to encrypt tokens
        private readonly SymmetricSecurityKey _encryptingKey;

        public TokenVerifierTests()
        {
            const string plainTextSecurityKey = "This is my shared, not so secret, secret that needs to be really long!";

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSecurityKey));
            _encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSecurityKey));

            _verifier = new TokenVerifier(_signingKey, _encryptingKey);
        }

        public class Constructor : TokenVerifierTests
        {
            [Fact]
            public void GivenNullSigningKey_ShouldThrowArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new TokenVerifier(null, _signingKey));
                exception.ParamName.Should().Be("signingKey");
            }

            [Fact]
            public void GivenNullEncryptingKey_ShouldThrowArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new TokenVerifier(_signingKey, null));
                exception.ParamName.Should().Be("encryptingKey");
            }

            [Fact]
            public void GivenSecurityKey_ShouldConfigureProperty()
            {
                _verifier.SigningKey.Should().Be(_signingKey);
            }
        }

        public class VirtualMachineIdProperty : TokenVerifierTests
        {
            [Fact]
            public void GivenUnconfiguredVerifier_IsEmpty()
            {
                _verifier.VirtualMachineId.Should().BeNullOrEmpty();
            }

            [Fact]
            public void ShouldBeConfiguredByModifier()
            {
                _verifier.WithVirtualMachineId(_virtualMachineId)
                    .VirtualMachineId.Should().Be(_virtualMachineId);
            }
        }

        public class WithCurrentInstant : TokenVerifierTests
        {
            [Fact]
            public void GivenUnconfiguredVerifier_IsCurrentInstant()
            {
                _verifier.CurrentInstant.Should().BeCloseTo(DateTimeOffset.Now, 100);
            }

            [Fact]
            public void ShouldBeConfiguredByModifier()
            {
                var instant = new DateTimeOffset(2014, 3, 27, 14, 29, 0, TimeSpan.FromHours(13));
                _verifier.WithCurrentInstant(instant)
                    .CurrentInstant.Should().Be(instant);
            }
        }
    }
}
