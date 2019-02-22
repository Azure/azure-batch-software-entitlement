using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class ResultTests
    {
        public class MatchWithAction : ResultTests
        {
            [Fact]
            public void WhenOk_CallsActionWithExpectedValue()
            {
                var test = new Result<int, string>(43);
                test.Match(
                    fromOk: v => v.Should().Be(43),
                    fromError: e => throw new InvalidOperationException("Should not be called"));
            }

            [Fact]
            public void WhenError_CallsActionWithExpectedError()
            {
                const string error = "Error";
                var test = new Result<int, string>(error);
                test.Match(
                    fromOk: v => throw new InvalidOperationException("Should not be called"),
                    fromError: e => e.Should().Contain(error));
            }
        }

        public class MatchMethodWithFunctions
        {
            [Fact]
            public void WhenSuccess_CallsFunctionWithExpectedValue()
            {
                var test = new Result<int, string>(43);
                test.Match(
                    fromOk: v =>
                    {
                        v.Should().Be(43);
                        return 128; // Needs a return value so this is a Func<int,int>
                    },
                    fromError: e => throw new InvalidOperationException("Should not be called"));
            }

            [Fact]
            public void WhenSuccess_ReturnsExpectedValueFromFunction()
            {
                var test = new Result<int, string>(43);
                var result = test.Match(
                    fromOk: v => 128,
                    fromError: e => throw new InvalidOperationException("Should not be called"));
                result.Should().Be(128);
            }

            [Fact]
            public void WhenFailure_CallsFunctionWithExpectedErrors()
            {
                const string error = "Error";
                var test = new Result<int, string>(error);
                test.Match(
                    fromOk: v => throw new InvalidOperationException("Should not be called"),
                    fromError: errors =>
                    {
                        errors.Should().BeEquivalentTo(error);
                        return 192; // Needs a return value so this is a Func<int,int> 
                    });
            }

            [Fact]
            public void WhenFailure_ReturnsExpectedValueFromFunction()
            {
                const string error = "Error";
                var test = new Result<int, string>(error);
                var result = test.Match(
                    fromOk: v => throw new InvalidOperationException("Should not be called"),
                    fromError: e => 192);
                result.Should().Be(192);
            }
        }
    }
}
