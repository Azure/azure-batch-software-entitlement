using System;
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
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void GivenValue_ReturnsResultWithNoErrors()
            {
                var result = Errorable.Success(42);
                result.Errors.Should().HaveCount(0);
            }
        }

        public class FailureMethod : ErrorableTests
        {
            [Fact]
            public void GivenSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable.Failure<int>("Error");
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable.Failure<int>("Error");
                result.Errors.Should().HaveCount(1);
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithoutValue()
            {
                var result = Errorable.Failure<int>(new List<string> { "Error" });
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void GivenListOfSingleValue_ReturnsResultWithOneError()
            {
                var result = Errorable.Failure<int>(new List<string> { "Error" });
                result.Errors.Should().HaveCount(1);
            }
        }

        public class AddErrorMethod : ErrorableTests
        {
            // Reference to a success to use for testing
            private readonly Errorable<int> _success = Errorable.Success(42);

            // Reference to a failure to use for testing
            private readonly Errorable<int> _failure = Errorable.Failure<int>("Error");

            [Fact]
            public void WhenSuccess_DiscardsValue()
            {
                var result = _success.AddError("Not the answer");
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenSuccess_IncludesError()
            {
                const string errorMessage = "Not the answer";
                var result = _success.AddError(errorMessage);
                result.Errors.Should().Contain(errorMessage);
            }

            [Fact]
            public void WhenFailure_IncludesError()
            {
                const string errorMessage = "Not the answer";
                var result = _failure.AddError(errorMessage);
                result.Errors.Should().Contain(errorMessage);
            }
        }

        public class AddErrorsMethod : ErrorableTests
        {
            // Reference to a success to use for testing
            private readonly Errorable<int> _success = Errorable.Success(42);

            // Reference to a failure to use for testing
            private readonly Errorable<int> _failure = Errorable.Failure<int>("Error");

            // A sequence of errors to test with
            private readonly IEnumerable<string> _errors = new List<string> {"err", "error"};

            [Fact]
            public void WhenSuccess_DiscardsValue()
            {
                var result = _success.AddErrors(_errors);
                result.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenSuccess_IncludesAllErrors()
            {
                var result = _success.AddErrors(_errors);
                result.Errors.Should().BeEquivalentTo(_errors);
            }

            [Fact]
            public void WhenFailure_IncludesAllErrors()
            {
                var result = _failure.AddErrors(_errors);
                result.Errors.Should().Contain(_errors);
            }
        }

        public class ValueProperty : ErrorableTests
        {
            [Fact]
            public void WhenSuccess_ReturnsExpectedValue()
            {
                var result = Errorable.Success(42);
                result.Value.Should().Be(42);
            }

            [Fact]
            public void WhenFailure_ThrowsInvalidOperationException()
            {
                var result = Errorable.Failure<int>("Error");
                Assert.Throws<InvalidOperationException>(
                    () => result.Value);
            }
        }

        public class MatchMethodWithActions
        {
            [Fact]
            public void WhenSuccess_CallsActionWithExpectedValue()
            {
                var errorable = Errorable.Success(43);
                errorable.Match(
                    v => v.Should().Be(43),
                    errors => throw new InvalidOperationException("Should not be called"));
            }

            [Fact]
            public void WhenFailure_CallsActionWithExpectedErrors()
            {
                const string error = "Error";
                var errorable = Errorable.Failure<int>(error);
                errorable.Match(
                    v => throw new InvalidOperationException("Should not be called"),
                    errors => errors.Should().Contain(error));
            }
        }

        public class MatchMethodWithFunctions
        {
            [Fact]
            public void WhenSuccess_CallsFunctionWithExpectedValue()
            {
                var errorable = Errorable.Success(43);
                var result = errorable.Match<int>(
                    v =>
                    {
                        v.Should().Be(43);
                        return 128; // Needs a return value so this is a Func<int,int> 
                    },
                    errors => throw new InvalidOperationException("Should not be called"));
            }

            [Fact]
            public void WhenSuccess_ReturnsExpectedValueFromFunction()
            {
                var errorable = Errorable.Success(43);
                var result = errorable.Match<int>(
                    v => 128,
                    errors => throw new InvalidOperationException("Should not be called"));
                result.Should().Be(128);
            }

            [Fact]
            public void WhenFailure_CallsFunctionWithExpectedErrors()
            {
                const string error = "Error";
                var errorable = Errorable.Failure<int>(error);
                var result = errorable.Match<int>(
                    v => throw new InvalidOperationException("Should not be called"),
                    errors =>
                    {
                        errors.Should().BeEquivalentTo(error);
                        return 192; // Needs a return value so this is a Func<int,int> 
                    });
            }

            [Fact]
            public void WhenFailure_ReturnsExpectedValueFromFunction()
            {
                const string error = "Error";
                var errorable = Errorable.Failure<int>(error);
                var result = errorable.Match<int>(
                    v => throw new InvalidOperationException("Should not be called"),
                    errors => 192);
                result.Should().Be(192);
            }
        }
    }
}
