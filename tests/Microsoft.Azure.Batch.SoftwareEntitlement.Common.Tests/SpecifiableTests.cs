using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class SpecifiableTests
    {
        private readonly Specifiable<string> _defaultOfString = default;

        public class Construction : SpecifiableTests
        {
            [Fact]
            public void CreatedAsParameterlessNew_IsNotSpecified()
            {
                var result = new Specifiable<string>();
                result.IsSpecified.Should().BeFalse();
            }

            [Fact]
            public void CreatedAsDefault_IsNotSpecified()
            {
                var result = _defaultOfString;
                result.IsSpecified.Should().BeFalse();
            }

            [Fact]
            public void CreatedWithValue_IsSpecified()
            {
                const string value = "test";
                var result = new Specifiable<string>(value);
                result.IsSpecified.Should().BeTrue();
            }

            [Fact]
            public void CreatedWithNull_IsSpecified()
            {
                const string value = null;
                var result = new Specifiable<string>(value);
                result.IsSpecified.Should().BeTrue();
            }

            [Fact]
            public void CreatedBySpecifyAsValue_IsSpecified()
            {
                const string value = "test";
                var result = Specify.As(value);
                result.IsSpecified.Should().BeTrue();
            }

            [Fact]
            public void CreatedBySpecifyAsNull_IsSpecified()
            {
                const string value = null;
                var result = Specify.As(value);
                result.IsSpecified.Should().BeTrue();
            }
        }

        public class OrDefault : SpecifiableTests
        {
            [Fact]
            public void WhenSpecifiedAsValue_ReturnsInstanceValue()
            {
                const string thisValue = "this";
                const string otherValue = "other";
                var result = Specify.As(thisValue).OrDefault(otherValue);
                result.Should().Be(thisValue);
            }

            [Fact]
            public void WhenSpecifiedAsNull_ReturnsNull()
            {
                const string thisValue = null;
                const string otherValue = "other";
                var result = Specify.As(thisValue).OrDefault(otherValue);
                result.Should().BeNull();
            }

            [Fact]
            public void WhenNotSpecified_ReturnsOtherValue()
            {
                const string otherValue = "other";
                var result = _defaultOfString.OrDefault(otherValue);
                result.Should().Be(otherValue);
            }
        }
    }
}
