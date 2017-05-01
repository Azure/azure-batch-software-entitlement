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
            const string keyForSigning = "This is my shared, not so secret, secret that needs to be really long!";
            const string keyForEncryption = "This is my shared, not so secret, secret that also needs to be really long!";

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyForSigning));
            _encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyForEncryption));

            _verifier = new TokenVerifier()
                .ConfigureOptionalSigningKey(_signingKey)
                .ConfigureOptionalEncryptionKey(_encryptingKey);
        }

        public class ConfigureOptionalSigningKey : TokenVerifierTests
        {
            [Fact]
            public void GivenNull_ReturnsExistingInstance()
            {
                var verifier = new TokenVerifier();
                verifier.ConfigureOptionalSigningKey(null)
                    .Should().BeSameAs(verifier);
            }

            [Fact]
            public void GivenSigningKey_ConfiguresProperty()
            {
                _verifier.ConfigureOptionalSigningKey(_signingKey)
                    .SigningKey.Should().BeSameAs(_signingKey);
            }

            [Fact]
            public void GivenNullWhenSigningKeyConfigured_ThrowsException()
            {
                Assert.Throws<InvalidOperationException>(
                    () => _verifier.ConfigureOptionalSigningKey(null));
            }
        }

        public class ConfigureOptionalEncryptionKey : TokenVerifierTests
        {
            [Fact]
            public void GivenNull_ReturnsExistingInstance()
            {
                var verifier = new TokenVerifier();
                verifier.ConfigureOptionalEncryptionKey(null)
                    .Should().BeSameAs(verifier);
            }

            [Fact]
            public void GivenSigningKey_ConfiguresProperty()
            {
                _verifier.ConfigureOptionalEncryptionKey(_encryptingKey)
                    .EncryptionKey.Should().BeSameAs(_encryptingKey);
            }

            [Fact]
            public void GivenNullWhenSigningKeyConfigured_ThrowsException()
            {
                Assert.Throws<InvalidOperationException>(
                    () => _verifier.ConfigureOptionalEncryptionKey(null));
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
