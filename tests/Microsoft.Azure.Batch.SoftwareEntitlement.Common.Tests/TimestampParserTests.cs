using System;
using FluentAssertions;
using NSubstitute;
using Serilog;
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
            public void GivenEmptyString_ReturnsNone()
            {
                var result = _parser.Parse(string.Empty);
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenTimestampInExpectedFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString(TimestampParser.ExpectedFormat);
                var result = _parser.Parse(valueToParse);
                result.Apply(
                    whenSome: d => d.Should().Be(_timestamp),
                    whenNone: () => Assert.True(false, "Expect to have a value"));
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString("G");
                var result = _parser.Parse(valueToParse);
                result.Apply(
                    whenSome: d => d.Should().Be(_timestamp),
                    whenNone: () => Assert.True(false, "Expect to have a value"));
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_GeneratesWarning()
            {
                var valueToParse = _timestamp.ToString("G");
                _parser.Parse(valueToParse);

                // Slightly nasty because we need to match argument types to select the right overload
                _logger.Received().Warning(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<DateTimeOffset>());
            }
        }
    }
}
