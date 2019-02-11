using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class ErrorSetTests
    {
        protected string NullString => null;

        protected IEnumerable<string> NullCollection => null;

        public class CreateWithErrors : ErrorSetTests
        {
            [Fact]
            public void GivenSingleError_ReturnsSingleResult()
            {
                var result = ErrorSet.Create("Error");
                result.Count().Should().Be(1);
                result.First().Should().Be("Error");
            }

            [Fact]
            public void GivenNullString_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => ErrorSet.Create(NullString));
                exception.ParamName.Should().Be("error");
            }

            [Fact]
            public void GivenNullEnumerable_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => ErrorSet.Create(NullCollection));
                exception.ParamName.Should().Be("errors");
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsSingleResult()
            {
                var result = ErrorSet.Create(new List<string> { "Error" });
                result.Count().Should().Be(1);
                result.First().Should().Be("Error");
            }

            [Fact]
            public void GivenListOfTwoValues_ReturnsTwoResults()
            {
                var result = ErrorSet.Create(new List<string> { "Error1", "Error2" });
                result.Count().Should().Be(2);
                result.Should().Contain("Error1");
                result.Should().Contain("Error2");
            }

            [Fact]
            public void GivenDuplicateValues_ReturnsOnlyOne()
            {
                var result = ErrorSet.Create("Error", "Error");
                result.Count().Should().Be(1);
                result.Should().Contain("Error");
            }
        }

        public class CreateEmpty : ErrorSetTests
        {
            [Fact]
            public void CreatesEmptyCollection()
            {
                var result = ErrorSet.Empty;
                result.Count().Should().Be(0);
            }
        }

        public class Combine : ErrorSetTests
        {
            private static readonly ErrorSet Empty = ErrorSet.Empty;
            private static readonly ErrorSet Null = null;
            private static readonly ErrorSet SingleItem = ErrorSet.Create("S1");
            private static readonly ErrorSet TwoItems = ErrorSet.Create("T1", "T2");

            [Fact]
            public void WithNull_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Empty.Combine(Null));
                exception.ParamName.Should().Be("combinable");
            }

            [Fact]
            public void EmptyWithEmpty_ReturnsEmpty()
            {
                var result = Empty.Combine(Empty);
                result.Count().Should().Be(0);
            }

            [Fact]
            public void EmptyWithSingle_ReturnsSingle()
            {
                var result = Empty.Combine(SingleItem);
                result.Count().Should().Be(1);
                result.First().Should().Be("S1");
            }

            [Fact]
            public void SingleWithEmpty_ReturnsSingle()
            {
                var result = SingleItem.Combine(Empty);
                result.Count().Should().Be(1);
                result.First().Should().Be("S1");
            }

            [Fact]
            public void SingleWithSameSingle_ReturnsSameSingle()
            {
                var result = SingleItem.Combine(Empty);
                result.Count().Should().Be(1);
                result.First().Should().Be("S1");
            }

            [Fact]
            public void SingleWithTwo_ReturnsAll()
            {
                var result = SingleItem.Combine(TwoItems);
                result.Count().Should().Be(3);
                result.Contains("S1").Should().BeTrue();
                result.Contains("T1").Should().BeTrue();
                result.Contains("T2").Should().BeTrue();
            }
        }
    }
}
