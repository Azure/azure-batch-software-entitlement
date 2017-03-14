using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class ErrorableTests
    {
        public class SuccessMethod : ErrorableTests
        {
            [Fact]
            public void GivenValue_ReturnsResultWithValue()
            {
                var result = Errorable<int>.Success(42);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void GivenValue_ReturnsResultWithNoErrors()
            {
                var result = Errorable<int>.Success(42);
                result.Errors.Should().HaveCount(0);
            }
        }

        public class FailureMethod : ErrorableTests
        {
            [Fact]
            public void GivenSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable<int>.Failure("Error");
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable<int>.Failure("Error");
                result.Errors.Should().HaveCount(1);
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable<int>.Failure(new List<string> {"Error"});
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable<int>.Failure(new List<string> { "Error" });
                result.Errors.Should().HaveCount(1);
            }
        }

        public class AddErrorMethod : ErrorableTests
        {
            // Reference to a success to use for testing
            private readonly Errorable<int> _success = Errorable<int>.Success(42);

            // Reference to a failure to use for testing
            private readonly Errorable<int> _failure = Errorable<int>.Failure("Error");

            [Fact]
            public void WhenSuccess_DiscardsValue()
            {
                var result = _success.AddError("Not the answer");
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenSuccess_IncludesError()
            {
                var errorMessage = "Not the answer";
                var result = _success.AddError(errorMessage);
                result.Errors.Should().Contain(errorMessage);
            }

            [Fact]
            public void WhenFailure_IncludesError()
            { 
                var errorMessage = "Not the answer";
                var result = _failure.AddError(errorMessage);
                result.Errors.Should().Contain(errorMessage);
            }
        }
    }
}
