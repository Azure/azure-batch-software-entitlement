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

        public class EqualsMethod : SomeTests
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
            public void GivenEquivilent_ReturnsTrue()
            {
                var other = Option.Some("sample");
                _option.Equals(other).Should().BeTrue();
            }
        }
    }
}
