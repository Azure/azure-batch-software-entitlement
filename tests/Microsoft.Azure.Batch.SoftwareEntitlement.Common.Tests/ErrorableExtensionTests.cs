using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    /// <summary>
    /// Tests for the extension methods available for <see cref="Errorable{T}"/>
    /// </summary>
    public abstract class ErrorableExtensionTests
    {
        // Known errorable values for testing
        private readonly Errorable<int> _missing = null;
        private readonly Errorable<int> _success = Errorable.Success(4);
        private readonly Errorable<int> _failure = Errorable.Failure<int>("failure");
        private readonly Errorable<int> _otherSuccess = Errorable.Success(2);
        private readonly Errorable<int> _otherFailure = Errorable.Failure<int>("other failure");
        private readonly Errorable<int> _yetAnotherSuccess = Errorable.Success(8);
        private readonly Errorable<int> _yetAnotherFailure = Errorable.Failure<int>("yet another failure");

        public class CombineWithErrorableAndActions : ErrorableExtensionTests
        {
            [Fact]
            public void GivenNullForFirstErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _missing.Combine(_otherSuccess, WhenSuccessfulDoNothing, WhenFailureDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_missing, WhenSuccessfulDoNothing, WhenFailureDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, null, WhenFailureDoNothing));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void GivenNullForFailureAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, WhenSuccessfulDoNothing, null));
                exception.ParamName.Should().Be("whenFailure");
            }

            [Fact]
            public void GivenSuccessAndSuccess_CallsSuccessActionWithExpectedValues()
            {
                var receivedLeft = 0;
                var receivedRight = 0;

                void WhenSuccessful(int left, int right)
                {
                    receivedLeft = left;
                    receivedRight = right;
                }

                _success.Combine(_otherSuccess, WhenSuccessful, WhenFailureAbort);

                receivedLeft.Should().Be(_success.Value);
                receivedRight.Should().Be(_otherSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndFailure_CallsFailureActionWithExistingErrors()
            {
                var receivedErrors = new List<string>();

                void WhenFailure(IEnumerable<string> errors)
                {
                    receivedErrors.AddRange(errors);
                }

                _success.Combine(_otherFailure, WhenSuccessfulAbort, WhenFailure);

                receivedErrors.Should().BeEquivalentTo(_otherFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccess_CallsFailureActionWithExistingErrors()
            {
                var receivedErrors = new List<string>();

                void WhenFailure(IEnumerable<string> errors)
                {
                    receivedErrors.AddRange(errors);
                }

                _failure.Combine(_otherSuccess, WhenSuccessfulAbort, WhenFailure);

                receivedErrors.Should().BeEquivalentTo(_failure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailure_CallsFailureActionWithCombinedErrors()
            {
                var receivedErrors = new List<string>();

                void WhenFailure(IEnumerable<string> errors)
                {
                    receivedErrors.AddRange(errors);
                }

                _failure.Combine(_otherFailure, WhenSuccessfulAbort, WhenFailure);

                receivedErrors.Should().BeEquivalentTo(_failure.Errors.Union(_otherFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var receivedErrors = new List<string>();

                void WhenFailure(IEnumerable<string> errors)
                {
                    receivedErrors.AddRange(errors);
                }

                _failure.Combine(_failure, WhenSuccessfulAbort, WhenFailure);

                receivedErrors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private void WhenSuccessfulDoNothing(int left, int right)
            {
            }

            // A safe default action for when a test doesn't care about failure
            private void WhenFailureDoNothing(IEnumerable<string> errors)
            {
            }

            // A success action that throws an exception to fail a test
            private void WhenSuccessfulAbort(int left, int right)
            {
                throw new InvalidOperationException();
            }

            // A failure action that throws an exception to fail a test
            private void WhenFailureAbort(IEnumerable<string> errors)
            {
                throw new InvalidOperationException();
            }
        }

        public class CombineWithErrorableAndFuncs : ErrorableExtensionTests
        {
            [Fact]
            public void GivenNullForFirstErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _missing.Combine(_otherSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_missing, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, (Func<int, int, int>)null));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void GivenSuccessAndSuccess_CallsSuccessActionWithExpectedValues()
            {
                var receivedLeft = 0;
                var receivedRight = 0;

                int WhenSuccessful(int left, int right)
                {
                    receivedLeft = left;
                    receivedRight = right;
                    return (left * 10) + right;
                }

                _success.Combine(_otherSuccess, WhenSuccessful);

                receivedLeft.Should().Be(_success.Value);
                receivedRight.Should().Be(_otherSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndSuccess_ReturnsExpectedResult()
            {
                int WhenSuccessful(int alpha, int beta) => (alpha * 10) + beta;

                var result = _success.Combine(_otherSuccess, WhenSuccessful);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(42);
            }

            [Fact]
            public void GivenSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherFailure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_otherFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _failure.Combine(_otherSuccess, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _failure.Combine(_otherFailure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors.Union(_otherFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _failure.Combine(_failure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int left, int right) => 0;

            // A success action that throws an exception to fail a test
            private int WhenSuccessfulAbort(int left, int right) => throw new InvalidOperationException();
        }

        public class CombineWithTwoErrorablesAndFuncs : ErrorableExtensionTests
        {
            [Fact]
            public void GivenNullForFirstErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _missing.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_missing, _yetAnotherSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForThirdErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, _missing, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("third");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                Func<int, int, int, string> whenSuccessful = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, _yetAnotherSuccess, whenSuccessful));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void GivenSuccessAndSuccessAndSuccess_CallsSuccessActionWithExpectedValues()
            {
                var receivedAlpha = 0;
                var receivedBeta = 0;
                var receivedGamma = 0;

                int WhenSuccessful(int alpha, int beta, int gamma)
                {
                    receivedAlpha = alpha;
                    receivedBeta = beta;
                    receivedGamma = gamma;
                    return 0;
                }

                _success.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessful);

                receivedAlpha.Should().Be(_success.Value);
                receivedBeta.Should().Be(_otherSuccess.Value);
                receivedGamma.Should().Be(_yetAnotherSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndSuccess_ReturnsExpectedResult()
            {
                int WhenSuccessful(int alpha, int beta, int gamma) => (alpha * 100) + (beta * 10) + gamma;

                var result = _success.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessful);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(428);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherSuccess, _yetAnotherFailure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_yetAnotherFailure.Errors);
            }

            [Fact]
            public void GivenSuccessAndFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherFailure, _yetAnotherSuccess, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_otherFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccessAndSuccess_ReturnsExistingErrors()
            {
                var result = _failure.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _failure.Combine(_otherFailure, _yetAnotherFailure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(
                    _failure.Errors.Union(_otherFailure.Errors).Union(_yetAnotherFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _failure.Combine(_failure, _failure, WhenSuccessfulAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int alpha, int beta, int gamma) => 0;

            // A success action that throws an exception to fail a test
            private int WhenSuccessfulAbort(int alpha, int beta, int gamma)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
