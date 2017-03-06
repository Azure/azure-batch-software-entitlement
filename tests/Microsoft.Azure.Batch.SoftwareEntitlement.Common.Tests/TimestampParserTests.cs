using System;
using System.Runtime.InteropServices.ComTypes;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class TimestampParserTests
    {
        // Substitute logger used for testing
        private readonly ISimpleLogger _logger = Substitute.For<ISimpleLogger>();

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
                var valid = _parser.TryParse(string.Empty, out var result);
                valid.Should().BeFalse();
            }

            [Fact]
            public void GivenTimestampInExpectedFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString(TimestampParser.ExpectedFormat);
                var valid = _parser.TryParse(valueToParse, out var result);
                valid.Should().BeTrue();
                result.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString("G");
                var valid = _parser.TryParse(valueToParse, out var result);
                valid.Should().BeTrue();
                result.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_GeneratesWarning()
            {
                var valueToParse = _timestamp.ToString("G");
                _parser.TryParse(valueToParse, out var result);

                _logger.Received().Warning(
                    Arg.Any<string>(),
                    Arg.Any<object[]>());
            }
        }
    }
}
