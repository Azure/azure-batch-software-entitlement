using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    /// <summary>
    /// Tests for the extension methods available for <see cref="Errorable{T}"/>
    /// </summary>
    public class ErrorableExtensionTests
    {
        public class CombineWithErrorableAndActions : ErrorableExtensionTests
        {
            // Known errorable values for testing
            private readonly Errorable<int> _missing = null;
            private readonly Errorable<int> _success = Errorable.Success(4);
            private readonly Errorable<int> _failure = Errorable.Failure<int>("failure");
            private readonly Errorable<int> _otherSuccess = Errorable.Success(2);
            private readonly Errorable<int> _otherFailure = Errorable.Failure<int>("other failure");

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

                _success.Combine(_otherFailure, WhenSuccessfullAbort, WhenFailure);

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

                _failure.Combine(_otherSuccess, WhenSuccessfullAbort, WhenFailure);

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

                _failure.Combine(_otherFailure, WhenSuccessfullAbort, WhenFailure);

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

                _failure.Combine(_failure, WhenSuccessfullAbort, WhenFailure);

                receivedErrors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private void WhenSuccessfulDoNothing(int left, int right) { }

            // A safe default action for when a test doesn't care about failure
            private void WhenFailureDoNothing(IEnumerable<string> errors) { }

            // A success action that throws an exception to fail a test
            private void WhenSuccessfullAbort(int left, int right)
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
            // Known errorable values for testing
            private readonly Errorable<int> _missing = null;
            private readonly Errorable<int> _success = Errorable.Success(4);
            private readonly Errorable<int> _failure = Errorable.Failure<int>("failure");
            private readonly Errorable<int> _otherSuccess = Errorable.Success(2);
            private readonly Errorable<int> _otherFailure = Errorable.Failure<int>("other failure");

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
                var result = _success.Combine(_otherSuccess, (l, r) => l * 10 + r);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(42);
            }

            [Fact]
            public void GivenSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_otherFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _failure.Combine(_otherSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _failure.Combine(_otherFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors.Union(_otherFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _failure.Combine(_failure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int left, int right) => 0;

            // A success action that throws an exception to fail a test
            private int WhenSuccessfullAbort(int left, int right) => throw new InvalidOperationException();
        }

        public class CombineWithTwoErrorablesAndFuncs : ErrorableExtensionTests
        {
            // Known errorable values for testing
            private readonly Errorable<int> _missing = null;
            private readonly Errorable<int> _success = Errorable.Success(4);
            private readonly Errorable<int> _failure = Errorable.Failure<int>("failure");
            private readonly Errorable<int> _otherSuccess = Errorable.Success(2);
            private readonly Errorable<int> _otherFailure = Errorable.Failure<int>("other failure");
            private readonly Errorable<int> _yetAnotherSuccess = Errorable.Success(8);
            private readonly Errorable<int> _yetAnotherFailure = Errorable.Failure<int>("yet another failure");

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
                var receivedFu = 0;
                var receivedBar = 0;
                var receivedBaz = 0;

                int WhenSuccessful(int fu, int bar, int baz)
                {
                    receivedFu = fu;
                    receivedBar = bar;
                    receivedBaz = baz;
                    return (fu * 100) + (bar * 10) + baz;
                }

                _success.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessful);

                receivedFu.Should().Be(_success.Value);
                receivedBar.Should().Be(_otherSuccess.Value);
                receivedBaz.Should().Be(_yetAnotherSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndSuccess_ReturnsExpectedResult()
            {
                int WhenSuccessful(int fu, int bar, int baz)
                {
                    return fu * 100 + bar * 10 + baz;
                }

                var result = _success.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessful);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(428);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherSuccess, _yetAnotherFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_yetAnotherFailure.Errors);
            }

            [Fact]
            public void GivenSuccessAndFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _success.Combine(_otherFailure, _yetAnotherSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_otherFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccessAndSuccess_ReturnsExistingErrors()
            {
                var result = _failure.Combine(_otherSuccess, _yetAnotherSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _failure.Combine(_otherFailure, _yetAnotherFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(
                    _failure.Errors.Union(_otherFailure.Errors).Union(_yetAnotherFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _failure.Combine(_failure, _failure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_failure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int alpha, int beta, int gamma) => 0;
            
            // A success action that throws an exception to fail a test
            private int WhenSuccessfullAbort(int alpha, int beta, int gamma)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
