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

        public class EqualsObject : SpecifiableTests
        {
            private readonly Specifiable<string> _sample = Specify.As("sample");

            private readonly Specifiable<string> _other = Specify.As("other");

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                _sample.Equals((object) _sample).Should().BeTrue();
            }

            [Fact]
            public void GivenOther_ReturnsFalse()
            {
                _sample.Equals((object)_other).Should().BeFalse();
            }

            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _sample.Equals(null).Should().BeFalse();
            }
        }

        public class EqualsSpecifiable : SpecifiableTests
        {
            private readonly Specifiable<string> _sample = Specify.As("sample");

            private readonly Specifiable<string> _other = Specify.As("other");

            private readonly Specifiable<string> _null = Specify.As<string>(null);

            private readonly Specifiable<string> _unspecified = new Specifiable<string>();

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                _sample.Equals(_sample).Should().BeTrue();
            }

            [Fact]
            public void GivenOther_ReturnsFalse()
            {
                _sample.Equals(_other).Should().BeFalse();
            }

            [Fact]
            public void GivenUnspecified_ReturnsFalse()
            {
                _sample.Equals(_unspecified).Should().BeFalse();
            }

            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _sample.Equals(null).Should().BeFalse();
            }

            [Fact]
            public void ContainingNullGivenSelf_ReturnsTrue()
            {
                _null.Equals(_null).Should().BeTrue();
            }

            [Fact]
            public void ContainingNullGivenOther_ReturnsFalse()
            {
                _null.Equals(_other).Should().BeFalse();
            }

            [Fact]
            public void ContainingNullGivenUnspecified_ReturnsFalse()
            {
                _null.Equals(_unspecified).Should().BeFalse();
            }

            [Fact]
            public void WhenUnspecified_EqualsSelf()
            {
                _unspecified.Equals(_unspecified).Should().BeTrue();
            }

            [Fact]
            public void WhenUnspecified_DoesNotEqualOther()
            {
                _unspecified.Equals(_other).Should().BeFalse();
            }

            [Fact]
            public void WhenUnspecified_DoesNotEqualNull()
            {
                _unspecified.Equals(_null).Should().BeFalse();
            }
        }
    }
}
