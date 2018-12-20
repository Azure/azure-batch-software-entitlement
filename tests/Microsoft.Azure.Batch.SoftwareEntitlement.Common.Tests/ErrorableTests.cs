using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class ErrorableTests
    {
        public class SuccessMethod : ErrorableTests
        {
            [Fact]
            public void GivenValue_ReturnsResultWithValue()
            {
                var result = Errorable.Success(42);
                result.GetOk().Should().Be(42);
            }

            [Fact]
            public void GivenValue_ReturnsResultWithNoErrors()
            {
                var result = Errorable.Success(42);
                result.IsOk().Should().BeTrue();
            }
        }

        public class FailureMethod : ErrorableTests
        {
            [Fact]
            public void GivenSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable.Failure<int>("Error");
                result.IsOk().Should().BeFalse();
            }

            [Fact]
            public void GivenSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable.Failure<int>("Error");
                result.GetError().Should().HaveCount(1);
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable.Failure<int>(new List<string> { "Error" });
                result.IsOk().Should().BeFalse();
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable.Failure<int>(new List<string> { "Error" });
                result.GetError().Should().HaveCount(1);
            }
        }
    }
}
