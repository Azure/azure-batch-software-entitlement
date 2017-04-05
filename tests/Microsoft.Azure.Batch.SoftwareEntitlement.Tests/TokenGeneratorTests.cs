using System;
using System.Collections.Generic;
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
        // Key used to sign tokens
        private readonly SymmetricSecurityKey _signingKey;

        // Logger that does nothing
        private readonly ILogger _nullLogger = NullLogger.Instance;

        public TokenGeneratorTests()
        {
            const string plainTextSecurityKey = "This is my shared, not so secret, secret!";
            _signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSecurityKey));
        }

        public class Constructor : TokenGeneratorTests
        {
            [Fact]
            public void GivenNullKey_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new TokenGenerator(null, _nullLogger));
                exception.ParamName.Should().Be("signingKey");
            }

            [Fact]
            public void GivenNullLogger_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new TokenGenerator(_signingKey, null));
                exception.ParamName.Should().Be("logger");
            }

            [Fact]
            public void GivenKey_InitializesProperty()
            {
                var generator = new TokenGenerator(_signingKey, _nullLogger);
                generator.SigningKey.Should().Be(_signingKey);
            }
        }

        public class Generate : TokenGeneratorTests
        {
            private readonly TokenGenerator _generator;

            public Generate()
            {
                _generator = new TokenGenerator(_signingKey, _nullLogger);
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
