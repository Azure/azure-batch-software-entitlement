using System;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class JwtEntitlementParserTests
    {
        // Credentials used for encryption
        private readonly EncryptingCredentials _encryptingCredentials;

        // Credentials used for signing
        private readonly SigningCredentials _signingCredentials;

        // The entitlements used to generate the token
        private readonly NodeEntitlements _sourceEntitlements;

        public JwtEntitlementParserTests()
        {
            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextSigningKey = "This is my shared, not so secret, secret that needs to be very long!";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSigningKey));
            _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextEncryptionKey = "This is another, not so secret, secret that needs to be very long!";
            var encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextEncryptionKey));
            _encryptingCredentials = new EncryptingCredentials(encryptingKey, "dir", SecurityAlgorithms.Aes256CbcHmacSha512);

            _sourceEntitlements = NodeEntitlements.Build(FakeEntitlementPropertyProvider.CreateValid()).Value;
        }

        private string GenerateToken(NodeEntitlements entitlements)
        {
            var generator = new TokenGenerator(NullLogger.Instance, _signingCredentials, _encryptingCredentials);
            return generator.Generate(entitlements);
        }

        private JwtEntitlementParser CreateParser(string expectedAudience, string expectedIssuer)
        {
            return new JwtEntitlementParser(
                expectedAudience,
                expectedIssuer,
                _signingCredentials.Key,
                _encryptingCredentials.Key);
        }

        public class MalformedToken : JwtEntitlementParserTests
        {
            [Fact]
            public void WhenTokenMalformed_ReturnsError()
            {
                var entitlements = _sourceEntitlements;
                var parser = CreateParser(entitlements.Audience, entitlements.Issuer);
                var result = parser.Parse("notarealtoken");
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("not well formed"));
            }
        }

        public class TokenTimeSpan : JwtEntitlementParserTests
        {
            private readonly TimeSpan _oneWeek = TimeSpan.FromDays(7);

            private readonly TimeSpan _oneDay = TimeSpan.FromDays(1);

            // Current time - captured as a member so it doesn't change during a test
            private readonly DateTimeOffset _now = DateTimeOffset.Now;

            [Fact]
            public void WhenTokenHasExpired_ReturnsExpectedError()
            {
                var entitlements = _sourceEntitlements
                    .FromInstant(_now - _oneWeek)
                    .UntilInstant(_now - _oneDay);
                var token = GenerateToken(entitlements);
                var parser = CreateParser(entitlements.Audience, entitlements.Issuer);
                var result = parser.Parse(token);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("expired"));
            }

            [Fact]
            public void WhenTokenHasNotYetStarted_ReturnsExpectedError()
            {
                var entitlements = _sourceEntitlements
                    .FromInstant(_now + _oneDay)
                    .UntilInstant(_now + _oneWeek);
                var token = GenerateToken(entitlements);
                var parser = CreateParser(entitlements.Audience, entitlements.Issuer);
                var result = parser.Parse(token);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("will not be valid"));
            }
        }

        public class TokenIssuer : JwtEntitlementParserTests
        {
            private readonly string _expectedIssuer = "https://issuer.region.batch.azure.test";
            private readonly string _unexpectedIssuer = "https://not-the-expected-issuer.region.batch.azure.test";

            [Fact]
            public void WhenTokenContainsUnexpectedIssuer_ReturnsExpectedError()
            {
                var entitlements = _sourceEntitlements.WithIssuer(_unexpectedIssuer);
                var token = GenerateToken(entitlements);
                var parser = CreateParser(entitlements.Audience, _expectedIssuer);
                var result = parser.Parse(token);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("Invalid issuer"));
            }
        }

        public class TokenAudience : JwtEntitlementParserTests
        {
            private readonly string _expectedAudience = "https://audience.region.batch.azure.test";
            private readonly string _unexpectedAudience = "https://not-the-expected-audience.region.batch.azure.test";

            [Fact]
            public void WhenTokenContainsUnexpectedAudience_ReturnsExpectedError()
            {
                var entitlements = _sourceEntitlements.WithAudience(_unexpectedAudience);
                var token = GenerateToken(entitlements);
                var parser = CreateParser(_expectedAudience, entitlements.Issuer);
                var result = parser.Parse(token);
                result.HasValue.Should().BeFalse();
                result.Errors.Should().Contain(e => e.Contains("Invalid audience"));
            }
        }
    }
}
