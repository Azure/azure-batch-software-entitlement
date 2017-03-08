using System;
using System.Runtime.InteropServices.ComTypes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class TimestampParserTests
    {
        // Substitute logger used for testing
        private readonly ILogger _logger = Substitute.For<ILogger>();

        public class Constructor : TimestampParserTests
        {
            [Fact]
            public void GivenNullLogger_ThrowsException()
            {
                Assert.Throws<ArgumentNullException>(
                    () => new TimestampParser(null));
            }
        }

        public class ParseMethod : TimestampParserTests
        {
            // Initialized parser to use for testing
            private readonly TimestampParser _parser;

            // Timestamp for testing
            private readonly DateTimeOffset _timestamp;

            public ParseMethod()
            {
                _parser = new TimestampParser(_logger);
                _timestamp = DateTimeOffset.Now.At(14, 39); // Needs to have 0 seconds and 0 milliseconds
            }

            [Fact]
            public void GivenEmptyString_ReturnsFalse()
            {
                var (valid, timestamp) = _parser.TryParse(string.Empty);
                valid.Should().BeFalse();
            }

            [Fact]
            public void GivenTimestampInExpectedFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString(TimestampParser.ExpectedFormat);
                var (valid, timestamp) = _parser.TryParse(valueToParse);
                valid.Should().BeTrue();
                timestamp.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString("G");
                var (valid, timestamp) = _parser.TryParse(valueToParse);
                valid.Should().BeTrue();
                timestamp.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_GeneratesWarning()
            {
                var valueToParse = _timestamp.ToString("G");
                _parser.TryParse(valueToParse);

                _logger.Received().LogWarning(
                    Arg.Any<string>(),
                    Arg.Any<object[]>());
            }
        }
    }
}
