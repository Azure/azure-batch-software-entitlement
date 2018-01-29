using System;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class TimestampParserTests
    {
        public class ParseMethod : TimestampParserTests
        {
            // Initialized parser to use for testing
            private readonly TimestampParser _parser;

            // Timestamp for testing
            private readonly DateTimeOffset _timestamp;

            // Invalid timestamp for testing
            private readonly string _invalidTimestamp = "not a timestamp";

            // Name of the value we're parsing
            private readonly string _name = "Demo";

            public ParseMethod()
            {
                _parser = new TimestampParser();
                _timestamp = DateTimeOffset.Now.At(14, 39); // Needs to have 0 seconds and 0 milliseconds
            }

            [Fact]
            public void GivenEmptyValue_ReturnsError()
            {
                var result = _parser.TryParse(string.Empty, "name");
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenEmptyName_ReturnsError()
            {
                var result = _parser.TryParse("value", string.Empty);
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenTimestampInExpectedFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);
                var result = _parser.TryParse(valueToParse, _name);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenTimestampInDifferentFormat_ReturnsExpectedValue()
            {
                var valueToParse = _timestamp.ToString("G", CultureInfo.InvariantCulture);
                var result = _parser.TryParse(valueToParse, _name);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(_timestamp);
            }

            [Fact]
            public void GivenInvalidTimestamp_ReturnsError()
            {
                var result = _parser.TryParse(_invalidTimestamp, _name);
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenInvalidTimestamp_ReturnsErrorIncludingName()
            {
                var result = _parser.TryParse(_invalidTimestamp, _name);
                result.Errors.Should().Contain(e => e.Contains(_name));
            }
        }
    }
}
