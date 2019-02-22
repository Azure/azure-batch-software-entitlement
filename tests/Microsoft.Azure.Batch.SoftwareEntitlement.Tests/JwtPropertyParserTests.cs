using System;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class JwtPropertyParserTests
    {
        // Credentials used for encryption
        private readonly EncryptingCredentials _encryptingCredentials;

        // Credentials used for signing
        private readonly SigningCredentials _signingCredentials;

        // The properties used to generate the token
        private readonly EntitlementTokenProperties _sourceTokenProperties;

        private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(1);
        private static readonly DateTimeOffset TokenNotBefore = DateTimeOffset.Now.ToUniversalTime();
        private static readonly DateTimeOffset TokenNotAfter = TokenNotBefore.Add(TokenLifetime);

        public JwtPropertyParserTests()
        {
            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextSigningKey = "This is my shared, not so secret, secret that needs to be very long!";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSigningKey));
            _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            // Hard coded key for unit testing only; actual operation will use a cert
            const string plainTextEncryptionKey = "This is another, not so secret, secret that needs to be very long!";
            var encryptingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextEncryptionKey));
            _encryptingCredentials = new EncryptingCredentials(encryptingKey, "dir", SecurityAlgorithms.Aes256CbcHmacSha512);

            _sourceTokenProperties = EntitlementTokenProperties.Build(FakeTokenPropertyProvider.CreateDefault())
                .AssertOk()
                .FromInstant(TokenNotBefore).UntilInstant(TokenNotAfter);
        }

        private string GenerateToken(EntitlementTokenProperties tokenProperties)
        {
            var generator = new TokenGenerator(NullLogger.Instance, _signingCredentials, _encryptingCredentials);
            return generator.Generate(tokenProperties);
        }

        private JwtPropertyParser CreateParser(string expectedAudience, string expectedIssuer)
        {
            return new JwtPropertyParser(
                expectedAudience,
                expectedIssuer,
                _signingCredentials.Key,
                _encryptingCredentials.Key);
        }

        public class MalformedToken : JwtPropertyParserTests
        {
            [Fact]
            public void WhenTokenContainsOnlyOnePart_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("nodots");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenContainsTooManyParts_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("e30=.e30=.e30=.e30=");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenContainsInvalidCharacters_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("🤘.🤙.🤚");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenContainsInvalidBase64_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("a.a.a");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenContainsInvalidContent_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("bm90.Z29vZA==.YmFzZTY0");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenMissingHeader_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse(".e30=.e30=");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }

            [Fact]
            public void WhenTokenMissingPayload_ReturnsError()
            {
                var tokenProperties = _sourceTokenProperties;
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse("e30=..e30=");
                result.AssertError().Should().Contain(e => e.Contains("not well formed"));
            }
        }

        public class TokenTimeSpan : JwtPropertyParserTests
        {
            private readonly TimeSpan _oneWeek = TimeSpan.FromDays(7);

            private readonly TimeSpan _oneDay = TimeSpan.FromDays(1);

            // Current time - captured as a member so it doesn't change during a test
            private readonly DateTimeOffset _now = DateTimeOffset.Now;

            [Fact]
            public void WhenTokenHasExpired_ReturnsExpectedError()
            {
                var tokenProperties = _sourceTokenProperties
                    .FromInstant(_now - _oneWeek)
                    .UntilInstant(_now - _oneDay);
                var token = GenerateToken(tokenProperties);
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse(token);
                result.AssertError().Should().Contain(e => e.Contains("expired"));
            }

            [Fact]
            public void WhenTokenHasNotYetStarted_ReturnsExpectedError()
            {
                var tokenProperties = _sourceTokenProperties
                    .FromInstant(_now + _oneDay)
                    .UntilInstant(_now + _oneWeek);
                var token = GenerateToken(tokenProperties);
                var parser = CreateParser(tokenProperties.Audience, tokenProperties.Issuer);
                var result = parser.Parse(token);
                result.AssertError().Should().Contain(e => e.Contains("will not be valid"));
            }
        }

        public class TokenIssuer : JwtPropertyParserTests
        {
            private readonly string _expectedIssuer = "https://issuer.region.batch.azure.test";
            private readonly string _unexpectedIssuer = "https://not-the-expected-issuer.region.batch.azure.test";

            [Fact]
            public void WhenTokenContainsUnexpectedIssuer_ReturnsExpectedError()
            {
                var tokenProperties = _sourceTokenProperties.WithIssuer(_unexpectedIssuer);
                var token = GenerateToken(tokenProperties);
                var parser = CreateParser(tokenProperties.Audience, _expectedIssuer);
                var result = parser.Parse(token);
                result.AssertError().Should().Contain(e => e.Contains("Invalid issuer"));
            }
        }

        public class TokenAudience : JwtPropertyParserTests
        {
            private readonly string _expectedAudience = "https://audience.region.batch.azure.test";
            private readonly string _unexpectedAudience = "https://not-the-expected-audience.region.batch.azure.test";

            [Fact]
            public void WhenTokenContainsUnexpectedAudience_ReturnsExpectedError()
            {
                var tokenProperties = _sourceTokenProperties.WithAudience(_unexpectedAudience);
                var token = GenerateToken(tokenProperties);
                var parser = CreateParser(_expectedAudience, tokenProperties.Issuer);
                var result = parser.Parse(token);
                result.AssertError().Should().Contain(e => e.Contains("Invalid audience"));
            }
        }
    }
}
