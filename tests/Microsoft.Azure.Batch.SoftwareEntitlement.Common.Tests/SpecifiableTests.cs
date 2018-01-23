using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class SpecifiableTests
    {
        private readonly Specifiable<string> _defaultOfString = default;

        // Values to specify
        private readonly string _thisStringValue = "this";
        private readonly string _nullStringValue;

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
                var result = new Specifiable<string>(_thisStringValue);
                result.IsSpecified.Should().BeTrue();
            }

            [Fact]
            public void CreatedWithNull_IsSpecified()
            {
                var result = new Specifiable<string>(_nullStringValue);
                result.IsSpecified.Should().BeTrue();
            }
        }

        public class SpecifyAs : SpecifiableTests
        {
            [Fact]
            public void WithValue_IsSpecified()
            {
                var result = Specify.As(_thisStringValue);
                result.IsSpecified.Should().BeTrue();
            }

            [Fact]
            public void WithNull_IsSpecified()
            {
                var result = Specify.As(_nullStringValue);
                result.IsSpecified.Should().BeTrue();
            }
        }

        public class OrDefault : SpecifiableTests
        {
            private readonly string _otherStringValue = "other";

            [Fact]
            public void WhenSpecifiedAsValue_ReturnsInstanceValue()
            {
                var result = Specify.As(_thisStringValue).OrDefault(_otherStringValue);
                result.Should().Be(_thisStringValue);
            }

            [Fact]
            public void WhenSpecifiedAsNull_ReturnsNull()
            {
                var result = Specify.As(_nullStringValue).OrDefault(_otherStringValue);
                result.Should().BeNull();
            }

            [Fact]
            public void WhenNotSpecified_ReturnsOtherValue()
            {
                var result = _defaultOfString.OrDefault(_otherStringValue);
                result.Should().Be(_otherStringValue);
            }
        }
    }
}
