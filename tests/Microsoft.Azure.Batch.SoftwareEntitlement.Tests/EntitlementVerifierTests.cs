using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class EntitlementVerifierTests
    {
        // An entitlements object representing a complete set of claims
        private readonly NodeEntitlements _completeEntitlement;

        // An entitlement verification request which is valid for the above entitlement
        private readonly EntitlementVerificationRequest _validEntitlementRequest;

        // Logger that does nothing
        private readonly ILogger _nullLogger = NullLogger.Instance;

        // Credentials used for encryption
        private readonly EncryptingCredentials _encryptingCredentials;

        // Credentials used for signing
        private readonly SigningCredentials _signingCredentials;

        // Used to look up values in a fake entitlement parser
        private readonly string _testToken = "testtoken";

        public EntitlementVerifierTests()
        {
            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextSigningKey = "This is my shared, not so secret, secret that needs to be very long!";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSigningKey));
            _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextEncryptionKey = "This is another, not so secret, secret that needs to be very long!";
            var encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextEncryptionKey));
            _encryptingCredentials = new EncryptingCredentials(encryptingKey, "dir", SecurityAlgorithms.Aes256CbcHmacSha512);

            _completeEntitlement = NodeEntitlements.Build(FakeEntitlementPropertyProvider.CreateValid()).Value;
            _validEntitlementRequest = new EntitlementVerificationRequest(
                _completeEntitlement.Applications.First(),
                _completeEntitlement.IpAddresses.First());
        }

        private string CreateSignedEncryptedJwtToken(NodeEntitlements entitlement)
            => CreateJwtToken(entitlement, _signingCredentials, _encryptingCredentials);

        private string CreateJwtToken(
            NodeEntitlements entitlement,
            SigningCredentials signingCredentials,
            EncryptingCredentials encryptingCredentials)
        {
            var generator = new TokenGenerator(_nullLogger, signingCredentials, encryptingCredentials);
            return generator.Generate(entitlement);
        }

        private EntitlementVerifier CreateSignedEncryptedJwtEntitlementVerifier(
            string expectedAudience,
            string expectedIssuer)
            => CreateJwtEntitlementVerifier(expectedAudience, expectedIssuer, _signingCredentials.Key, _encryptingCredentials.Key);

        private EntitlementVerifier CreateJwtEntitlementVerifier(
            string expectedAudience,
            string expectedIssuer,
            SecurityKey signingKey,
            SecurityKey encryptingKey)
        {
            var parser = new JwtEntitlementParser(expectedAudience, expectedIssuer, signingKey, encryptingKey);
            return new EntitlementVerifier(parser);
        }

        public class Verify : EntitlementVerifierTests
        {
            [Fact]
            public void WhenTokenIsNull_ThrowsException()
            {
                var verifier = new EntitlementVerifier(new FakeEntitlementParser());
                Assert.Throws<ArgumentNullException>(
                    () => verifier.Verify(_validEntitlementRequest, null));
            }
        }

        public class VerifyApplication : EntitlementVerifierTests
        {
            private readonly string _otherApp1 = "contosoit";
            private readonly string _otherApp2 = "contosohr";

            [Fact]
            public void WhenEntitlementContainsNoApplications_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithApplications();
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedApplication_ReturnsSuccess()
            {
                var entitlement = _completeEntitlement.WithApplications(_validEntitlementRequest.ApplicationId);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenEntitlementContainsOnlyADifferentApplication_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithApplications(_otherApp1);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsButNotTheRequestedApplication_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithApplications(_otherApp1, _otherApp2);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsIncludingTheRequestedApplication_ReturnsSuccess()
            {
                var entitlement = _completeEntitlement.WithApplications(
                    _validEntitlementRequest.ApplicationId, _otherApp1, _otherApp2);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeTrue();
            }
        }

        public class VerifyIpAddress : EntitlementVerifierTests
        {
            private readonly IPAddress _otherAddress1 = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _otherAddress2 = IPAddress.Parse("203.0.113.43");

            [Fact]
            public void WhenEntitlementContainsNoIpAddresses_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithIpAddresses();
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedIpAddress_ReturnsSuccess()
            {
                var entitlement = _completeEntitlement.WithIpAddresses(_validEntitlementRequest.IpAddress);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenEntitlementContainsOnlyADifferentIpAddress_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithIpAddresses(_otherAddress1);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleIpAddressesButNotTheRequestedOne_ReturnsError()
            {
                var entitlement = _completeEntitlement.WithIpAddresses(_otherAddress1, _otherAddress2);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains(_validEntitlementRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleIpAddressesIncludingTheRequestedOne_ReturnsSuccess()
            {
                var entitlement = _completeEntitlement.WithIpAddresses(
                    _validEntitlementRequest.IpAddress, _otherAddress1, _otherAddress2);
                var parser = new FakeEntitlementParser(_testToken, entitlement);
                var verifier = new EntitlementVerifier(parser);
                var result = verifier.Verify(_validEntitlementRequest, _testToken);
                result.HasValue.Should().BeTrue();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with both signing and encryption
        /// </summary>
        public class WithSigningAndEncryption : EntitlementVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateSignedEncryptedJwtEntitlementVerifier(_completeEntitlement.Audience, _completeEntitlement.Issuer);
                var token = CreateSignedEncryptedJwtToken(_completeEntitlement);
                var result = verifier.Verify(_validEntitlementRequest, token);
                result.HasValue.Should().BeTrue();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no signing key 
        /// </summary>
        public class WithEncryptionOnly : EntitlementVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtEntitlementVerifier(
                    _completeEntitlement.Audience,
                    _completeEntitlement.Issuer,
                    signingKey: null,
                    encryptingKey: _encryptingCredentials.Key);

                var token = CreateJwtToken(_completeEntitlement, signingCredentials: null, encryptingCredentials: _encryptingCredentials);

                var result = verifier.Verify(_validEntitlementRequest, token);
                result.HasValue.Should().BeTrue();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no encryption key 
        /// </summary>
        public class WithSigningOnly : EntitlementVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtEntitlementVerifier(
                    _completeEntitlement.Audience,
                    _completeEntitlement.Issuer,
                    signingKey: _signingCredentials.Key,
                    encryptingKey: null);

                var token = CreateJwtToken(_completeEntitlement, signingCredentials: _signingCredentials, encryptingCredentials: null);

                var result = verifier.Verify(_validEntitlementRequest, token);
                result.HasValue.Should().BeTrue();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end without signing or encryption
        /// </summary>
        public class WithoutSigningOrEncryption : EntitlementVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtEntitlementVerifier(
                    _completeEntitlement.Audience,
                    _completeEntitlement.Issuer,
                    signingKey: null,
                    encryptingKey: null);

                var token = CreateJwtToken(_completeEntitlement, signingCredentials: null, encryptingCredentials: null);

                var result = verifier.Verify(_validEntitlementRequest, token);
                result.HasValue.Should().BeTrue();
            }
        }

        public class WithCertificates : EntitlementVerifierTests
        {
            [Theory(Skip = "Specify a certificate thumbprint in TestCaseKeys() to enable this test.")]
            [MemberData(nameof(TestCaseKeys))]
            public void WhenSignedByCertificate_ReturnsExpectedResult(SecurityKey key)
            {
                // Arrange
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha512Signature);
                var generator = new TokenGenerator(_nullLogger, signingCredentials, encryptingCredentials: null);
                var verifier = CreateJwtEntitlementVerifier(
                    _completeEntitlement.Audience,
                    _completeEntitlement.Issuer,
                    signingKey : key,
                    encryptingKey: null);
                // Act
                var token = generator.Generate(_completeEntitlement);
                var result = verifier.Verify(_validEntitlementRequest, token);
                // Assert
                result.HasValue.Should().BeTrue();
                result.Errors.Should().BeEmpty();
            }

            [Theory(Skip = "Specify a certificate thumbprint in TestCaseKeys() to enable this test.")]
            [MemberData(nameof(TestCaseKeys))]
            public void WhenEncryptedByCertificate_ReturnsExpectedResult(SecurityKey key)
            {
                // Arrange
                var encryptingCredentials = new EncryptingCredentials(key, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512);
                var generator = new TokenGenerator(_nullLogger, signingCredentials: null, encryptingCredentials: encryptingCredentials);
                var verifier = CreateJwtEntitlementVerifier(
                    _completeEntitlement.Audience,
                    _completeEntitlement.Issuer,
                    signingKey: null,
                    encryptingKey: key);
                // Act
                var token = generator.Generate(_completeEntitlement);
                var result = verifier.Verify(_validEntitlementRequest, token);
                // Assert
                result.HasValue.Should().BeTrue();
                result.Errors.Should().BeEmpty();
            }

            public static IEnumerable<object[]> TestCaseKeys()
            {
                // To use this test, change the next line by entering a thumbprint that exists on the test machine
                var thumbprint = new CertificateThumbprint("<thumbprint-goes-here>");
                var store = new CertificateStore();
                var cert = store.FindByThumbprint("test", thumbprint);
                if (!cert.HasValue)
                {
                    throw new InvalidOperationException(cert.Errors.First());
                }

                var parameters = cert.Value.GetRSAPrivateKey().ExportParameters(includePrivateParameters: true);
                var key = new RsaSecurityKey(parameters);

                yield return new object[] { key };
            }
        }
    }
}
