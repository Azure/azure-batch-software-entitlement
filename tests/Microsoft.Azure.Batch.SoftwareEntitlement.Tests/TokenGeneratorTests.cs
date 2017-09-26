using System;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class TokenGeneratorTests
    {
        // Credentials used to sign tokens
        private readonly SigningCredentials _signingCredentials;

        // Credentials used to encrypt tokens
        private readonly EncryptingCredentials _encryptionCredentials;

        // Logger that does nothing
        private readonly ILogger _nullLogger = NullLogger.Instance;

        public TokenGeneratorTests()
        {
            const string plainTextSecurityKey = "This is my shared, not so secret, secret!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(plainTextSecurityKey));
            _signingCredentials = new SigningCredentials(key, "fu");
            _encryptionCredentials = new EncryptingCredentials(key, "bar", "baz");
        }

        public class Constructor : TokenGeneratorTests
        {
            [Fact]
            public void GivenNullLogger_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new TokenGenerator(null, _signingCredentials, _encryptionCredentials));
                exception.ParamName.Should().Be("logger");
            }

            [Fact]
            public void GivenSigningKey_InitializesProperty()
            {
                var generator = new TokenGenerator(_nullLogger, _signingCredentials, _encryptionCredentials);
                generator.SigningCredentials.Should().Be(_signingCredentials);
            }

            [Fact]
            public void GivenEncryptionKey_InitializesProperty()
            {
                var generator = new TokenGenerator(_nullLogger, _signingCredentials, _encryptionCredentials);
                generator.EncryptingCredentials.Should().Be(_encryptionCredentials);
            }
        }

        public class Generate : TokenGeneratorTests
        {
            private readonly TokenGenerator _generator;


            public Generate()
            {
                _generator = new TokenGenerator(_nullLogger, _signingCredentials, _encryptionCredentials);
            }

            [Fact]
            public void GivenNullEntitlement_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _generator.Generate(null));
                exception.ParamName.Should().Be("entitlements");
            }
        }
    }
}
