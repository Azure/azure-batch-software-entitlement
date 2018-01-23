using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class NoneTests
    {
        // Standard option to use for testing
        private readonly IOption<string> _option = Option.None<string>();

        public class WhenNone : NoneTests
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
            public void GivenAction_ExecutesAction()
            {
                var called = false;
                _option.WhenNone(() => called = true);
                called.Should().BeTrue();
            }
        }

        public class WhenSome : NoneTests
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
            public void GivenAction_DoesNotExecuteAction()
            {
                var called = false;
                _option.WhenSome(none => called = true);
                called.Should().BeFalse();
            }
        }

        public class Match : NoneTests
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
            public void GivenFunctions_CallsFunctionForNone()
            {
                var called = _option.Match(() => true, some => false);
                called.Should().BeTrue();
            }
        }

        public class OrDefault : NoneTests
        {
            private readonly string _default = "a default value";

            [Fact]
            public void GivenDefaultValue_ReturnsDefaultValue()
            {
                _option.OrDefault(_default).Should().Be(_default);
            }
        }

        public class EqualsOptionMethod : NoneTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _option.Equals(null).Should().BeFalse();
            }

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                _option.Equals(_option).Should().BeTrue();
            }

            [Fact]
            public void GivenEquivalent_ReturnsTrue()
            {
                var other = Option.None<string>();
                _option.Equals(other).Should().BeTrue();
            }
        }

        public class EqualsObjectMethod : NoneTests
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

