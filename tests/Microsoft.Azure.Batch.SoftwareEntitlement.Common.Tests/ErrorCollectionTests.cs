using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class ErrorCollectionTests
    {
        protected string NullString => null;

        protected IEnumerable<string> NullCollection => null;

        public class CreateWithErrors : ErrorCollectionTests
        {
            [Fact]
            public void GivenSingleError_ReturnsSingleResult()
            {
                var result = ErrorCollection.Create("Error");
                result.Count().Should().Be(1);
                result.First().Should().Be("Error");
            }

            [Fact]
            public void GivenNullString_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() => ErrorCollection.Create(NullString));
                exception.ParamName.Should().Be("error");
            }

            [Fact]
            public void GivenNullEnumerable_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() => ErrorCollection.Create(NullCollection));
                exception.ParamName.Should().Be("errors");
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsSingleResult()
            {
                var result = ErrorCollection.Create(new List<string> { "Error" });
                result.Count().Should().Be(1);
                result.First().Should().Be("Error");
            }

            [Fact]
            public void GivenListOfTwoValues_ReturnsTwoResults()
            {
                var result = ErrorCollection.Create(new List<string> { "Error1", "Error2" });
                result.Count().Should().Be(2);
                result.Should().Contain("Error1");
                result.Should().Contain("Error2");
            }

            [Fact]
            public void GivenDuplicateValues_ReturnsOnlyOne()
            {
                var result = ErrorCollection.Create("Error", "Error");
                result.Count().Should().Be(1);
                result.Should().Contain("Error");
            }
        }

        public class CreateEmpty : ErrorCollectionTests
        {
            [Fact]
            public void CreatesEmptyCollection()
            {
                var result = ErrorCollection.Empty;
                result.Count().Should().Be(0);
            }
        }

        public class Combine : ErrorCollectionTests
        {
            private static readonly ErrorCollection Empty = ErrorCollection.Empty;
            private static readonly ErrorCollection Null = null;
            private static readonly ErrorCollection SingleItem = ErrorCollection.Create("S1");
            private static readonly ErrorCollection TwoItems = ErrorCollection.Create("T1", "T2");

            [Fact]
            public void WithNull_ThrowsException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() => Empty.Combine(Null));
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
