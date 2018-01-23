using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class SomeTests
    {
        private readonly IOption<string> _option = Option.Some("sample");

        public class WhenNone : SomeTests
        {
            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _option.WhenNone(null));
                exception.ParamName.Should().Be("action");
            }

            [Fact]
            public void GivenAction_DoesNotExecuteAction()
            {
                var called = false;
                _option.WhenNone(() => called = true);
                called.Should().BeFalse();
            }
        }

        public class WhenSome : SomeTests
        {
            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _option.WhenSome(null));
                exception.ParamName.Should().Be("action");
            }

            [Fact]
            public void GivenAction_ExecutesAction()
            {
                var called = false;
                _option.WhenSome(none => called = true);
                called.Should().BeTrue();
            }
        }

        public class Match : SomeTests
        {
            [Fact]
            public void GivenNullForNoneFunc_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _option.Match(null, some => string.Empty));
                exception.ParamName.Should().Be("none");
            }

            [Fact]
            public void GivenNullForSomeFunc_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _option.Match(() => string.Empty, null));
                exception.ParamName.Should().Be("some");
            }

            [Fact]
            public void GivenFunctions_CallsFunctionForSome()
            {
                var called = _option.Match(() => false, some => true);
                called.Should().BeTrue();
            }
        }

        public class OrDefault : SomeTests
        {
            private readonly string _default = "a default value";

            [Fact]
            public void GivenDefaultValue_ReturnsActualValue()
            {
                _option.OrDefault(_default).Should().NotBe(_default);
            }
        }

        public class EqualsOptionMethod : SomeTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                string nullString = null;
                _option.Equals(nullString).Should().BeFalse();
            }

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                _option.Equals(_option).Should().BeTrue();
            }

            [Fact]
            public void GivenOther_ReturnsFalse()
            {
                var other = Option.Some("other");
                _option.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void GivenEquivalent_ReturnsTrue()
            {
                var other = Option.Some("sample");
                _option.Equals(other).Should().BeTrue();
            }
        }

        public class EqualsObjectMethod : SomeTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                object nullObject = null;
                _option.Equals(nullObject).Should().BeFalse();
            }

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                object option = _option;
                _option.Equals(option).Should().BeTrue();
            }

            [Fact]
            public void GivenOtherOption_ReturnsFalse()
            {
                object other = Option.Some("other");
                _option.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void GivenOtherType_ReturnsFalse()
            {
                _option.Equals(this).Should().BeFalse();
            }
        }
    }
}
