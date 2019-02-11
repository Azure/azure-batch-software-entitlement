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
    public class TokenVerifierTests
    {
        // An token properties object representing a complete set of claims
        private readonly EntitlementTokenProperties _completeTokenProperties;

        // An entitlement verification request which is valid for the above entitlement
        private readonly TokenVerificationRequest _validVerificationRequest;

        // Logger that does nothing
        private readonly ILogger _nullLogger = NullLogger.Instance;

        // Credentials used for encryption
        private readonly EncryptingCredentials _encryptingCredentials;

        // Credentials used for signing
        private readonly SigningCredentials _signingCredentials;

        // Used to look up values in a fake entitlement parser
        private readonly string _testToken = "testtoken";

        public TokenVerifierTests()
        {
            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextSigningKey = "This is my shared, not so secret, secret that needs to be very long!";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSigningKey));
            _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextEncryptionKey = "This is another, not so secret, secret that needs to be very long!";
            var encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextEncryptionKey));
            _encryptingCredentials = new EncryptingCredentials(encryptingKey, "dir", SecurityAlgorithms.Aes256CbcHmacSha512);

            _completeTokenProperties = EntitlementTokenProperties.Build(FakeTokenPropertyProvider.CreateValid()).AssertOk();
            _validVerificationRequest = new TokenVerificationRequest(
                _completeTokenProperties.Applications.First(),
                _completeTokenProperties.IpAddresses.First());
        }

        private string CreateSignedEncryptedJwtToken(EntitlementTokenProperties tokenProperties)
            => CreateJwtToken(tokenProperties, _signingCredentials, _encryptingCredentials);

        private string CreateJwtToken(
            EntitlementTokenProperties tokenProperties,
            SigningCredentials signingCredentials,
            EncryptingCredentials encryptingCredentials)
        {
            var generator = new TokenGenerator(_nullLogger, signingCredentials, encryptingCredentials);
            return generator.Generate(tokenProperties);
        }

        private TokenVerifier CreateSignedEncryptedJwtTokenVerifier(
            string expectedAudience,
            string expectedIssuer)
            => CreateJwtTokenVerifier(expectedAudience, expectedIssuer, _signingCredentials.Key, _encryptingCredentials.Key);

        private TokenVerifier CreateJwtTokenVerifier(
            string expectedAudience,
            string expectedIssuer,
            SecurityKey signingKey,
            SecurityKey encryptingKey)
        {
            var parser = new JwtPropertyParser(expectedAudience, expectedIssuer, signingKey, encryptingKey);
            return new TokenVerifier(parser);
        }

        public class Verify : TokenVerifierTests
        {
            [Fact]
            public void WhenTokenIsNull_ThrowsException()
            {
                var verifier = new TokenVerifier(new FakeTokenPropertyParser());
                Assert.Throws<ArgumentNullException>(
                    () => verifier.Verify(_validVerificationRequest, null));
            }
        }

        public class VerifyApplication : TokenVerifierTests
        {
            private readonly string _otherApp1 = "contosoit";
            private readonly string _otherApp2 = "contosohr";

            [Fact]
            public void WhenEntitlementContainsNoApplications_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithApplications();
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedApplication_ReturnsSuccess()
            {
                var tokenProperties = _completeTokenProperties.WithApplications(_validVerificationRequest.ApplicationId);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertOk();
            }

            [Fact]
            public void WhenEntitlementContainsOnlyADifferentApplication_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithApplications(_otherApp1);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsButNotTheRequestedApplication_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithApplications(_otherApp1, _otherApp2);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.ApplicationId));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleApplicationsIncludingTheRequestedApplication_ReturnsSuccess()
            {
                var tokenProperties = _completeTokenProperties.WithApplications(
                    _validVerificationRequest.ApplicationId, _otherApp1, _otherApp2);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertOk();
            }
        }

        public class VerifyIpAddress : TokenVerifierTests
        {
            private readonly IPAddress _otherAddress1 = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _otherAddress2 = IPAddress.Parse("203.0.113.43");

            [Fact]
            public void WhenEntitlementContainsNoIpAddresses_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithIpAddresses();
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsOnlyTheRequestedIpAddress_ReturnsSuccess()
            {
                var tokenProperties = _completeTokenProperties.WithIpAddresses(_validVerificationRequest.IpAddress);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertOk();
            }

            [Fact]
            public void WhenEntitlementContainsOnlyADifferentIpAddress_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithIpAddresses(_otherAddress1);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleIpAddressesButNotTheRequestedOne_ReturnsError()
            {
                var tokenProperties = _completeTokenProperties.WithIpAddresses(_otherAddress1, _otherAddress2);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertError().Should().Contain(e => e.Contains(_validVerificationRequest.IpAddress.ToString()));
            }

            [Fact]
            public void WhenEntitlementContainsMultipleIpAddressesIncludingTheRequestedOne_ReturnsSuccess()
            {
                var tokenProperties = _completeTokenProperties.WithIpAddresses(
                    _validVerificationRequest.IpAddress, _otherAddress1, _otherAddress2);
                var parser = new FakeTokenPropertyParser(_testToken, tokenProperties);
                var verifier = new TokenVerifier(parser);
                var result = verifier.Verify(_validVerificationRequest, _testToken);
                result.AssertOk();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with both signing and encryption
        /// </summary>
        public class WithSigningAndEncryption : TokenVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateSignedEncryptedJwtTokenVerifier(_completeTokenProperties.Audience, _completeTokenProperties.Issuer);
                var token = CreateSignedEncryptedJwtToken(_completeTokenProperties);
                var result = verifier.Verify(_validVerificationRequest, token);
                result.AssertOk();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no signing key 
        /// </summary>
        public class WithEncryptionOnly : TokenVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtTokenVerifier(
                    _completeTokenProperties.Audience,
                    _completeTokenProperties.Issuer,
                    signingKey: null,
                    encryptingKey: _encryptingCredentials.Key);

                var token = CreateJwtToken(_completeTokenProperties, signingCredentials: null, encryptingCredentials: _encryptingCredentials);

                var result = verifier.Verify(_validVerificationRequest, token);
                result.AssertOk();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end with no encryption key 
        /// </summary>
        public class WithSigningOnly : TokenVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtTokenVerifier(
                    _completeTokenProperties.Audience,
                    _completeTokenProperties.Issuer,
                    signingKey: _signingCredentials.Key,
                    encryptingKey: null);

                var token = CreateJwtToken(_completeTokenProperties, signingCredentials: _signingCredentials, encryptingCredentials: null);

                var result = verifier.Verify(_validVerificationRequest, token);
                result.AssertOk();
            }
        }

        /// <summary>
        /// Tests to check that enforcement works end to end without signing or encryption
        /// </summary>
        public class WithoutSigningOrEncryption : TokenVerifierTests
        {
            [Fact]
            public void GivenValidEntitlement_ReturnsSuccess()
            {
                var verifier = CreateJwtTokenVerifier(
                    _completeTokenProperties.Audience,
                    _completeTokenProperties.Issuer,
                    signingKey: null,
                    encryptingKey: null);

                var token = CreateJwtToken(_completeTokenProperties, signingCredentials: null, encryptingCredentials: null);

                var result = verifier.Verify(_validVerificationRequest, token);
                result.AssertOk();
            }
        }

        public class WithCertificates : TokenVerifierTests
        {
            [Theory(Skip = "Specify a certificate thumbprint in TestCaseKeys() to enable this test.")]
            [MemberData(nameof(TestCaseKeys))]
            public void WhenSignedByCertificate_ReturnsExpectedResult(SecurityKey key)
            {
                // Arrange
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha512Signature);
                var generator = new TokenGenerator(_nullLogger, signingCredentials, encryptingCredentials: null);
                var verifier = CreateJwtTokenVerifier(
                    _completeTokenProperties.Audience,
                    _completeTokenProperties.Issuer,
                    signingKey : key,
                    encryptingKey: null);
                // Act
                var token = generator.Generate(_completeTokenProperties);
                var result = verifier.Verify(_validVerificationRequest, token);
                // Assert
                result.AssertOk();
            }

            [Theory(Skip = "Specify a certificate thumbprint in TestCaseKeys() to enable this test.")]
            [MemberData(nameof(TestCaseKeys))]
            public void WhenEncryptedByCertificate_ReturnsExpectedResult(SecurityKey key)
            {
                // Arrange
                var encryptingCredentials = new EncryptingCredentials(key, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512);
                var generator = new TokenGenerator(_nullLogger, signingCredentials: null, encryptingCredentials: encryptingCredentials);
                var verifier = CreateJwtTokenVerifier(
                    _completeTokenProperties.Audience,
                    _completeTokenProperties.Issuer,
                    signingKey: null,
                    encryptingKey: key);
                // Act
                var token = generator.Generate(_completeTokenProperties);
                var result = verifier.Verify(_validVerificationRequest, token);
                // Assert
                result.AssertOk();
            }

            public static IEnumerable<object[]> TestCaseKeys()
            {
                // To use this test, change the next line by entering a thumbprint that exists on the test machine
                var thumbprint = new CertificateThumbprint("<thumbprint-goes-here>");
                var store = new CertificateStore();

                X509Certificate2 ThrowException(ErrorSet errors) =>
                    throw new InvalidOperationException(errors.First());

                var cert = store.FindByThumbprint("test", thumbprint).OnError(ThrowException).AssertOk();

                var parameters = cert.GetRSAPrivateKey().ExportParameters(includePrivateParameters: true);
                var key = new RsaSecurityKey(parameters);

                yield return new object[] { key };
            }
        }
    }
}
