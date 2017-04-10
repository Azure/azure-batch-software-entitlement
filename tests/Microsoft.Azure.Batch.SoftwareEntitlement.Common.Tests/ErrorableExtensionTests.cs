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
        // A safe default action for when a test doesn't care about success
        private void DefaultSuccessAction(int left, int right) { }

        // A safe default action for when a test doesn't care about failure
        private void DefaultFailureAction(IEnumerable<string> errors) { }

        // A success action that throws an exception to fail a test
        private void AbortSuccessAction(int left, int right)
        {
            throw new InvalidOperationException();
        }

        // A failure action that throws an exception to fail a test
        private void AbortFailureAction(IEnumerable<string> errors)
        {
            throw new InvalidOperationException();
        }

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
                        () => _nullErrorable.Combine(_secondSuccess, WhenSuccessfulDoNothing, WhenFailureDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_nullErrorable, WhenSuccessfulDoNothing, WhenFailureDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_secondSuccess, null, WhenFailureDoNothing));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void GivenNullForFailureAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_secondSuccess, WhenSuccessfulDoNothing, null));
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
            private readonly Errorable<int> _nullErrorable = null;
            private readonly Errorable<int> _firstSuccess = Errorable.Success(4);
            private readonly Errorable<int> _firstFailure = Errorable.Failure<int>("Failure the first");
            private readonly Errorable<int> _secondSuccess = Errorable.Success(2);
            private readonly Errorable<int> _secondFailure = Errorable.Failure<int>("Failure the second");

            [Fact]
            public void GivenNullForFirstErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _nullErrorable.Combine(_secondSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_nullErrorable, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_secondSuccess, (Func<int, int, int>)null));
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

                _firstSuccess.Combine(_secondSuccess, WhenSuccessful);

                receivedLeft.Should().Be(_firstSuccess.Value);
                receivedRight.Should().Be(_secondSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndSuccess_ReturnsExpectedResult()
            {
                var result = _firstSuccess.Combine(_secondSuccess, (l, r) => l * 10 + r);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(42);
            }

            [Fact]
            public void GivenSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _firstSuccess.Combine(_secondFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_secondFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _firstFailure.Combine(_secondSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_firstFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _firstFailure.Combine(_secondFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_firstFailure.Errors.Union(_secondFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _firstFailure.Combine(_firstFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_firstFailure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int left, int right) => 0;

            // A success action that throws an exception to fail a test
            private int WhenSuccessfullAbort(int left, int right) => throw new InvalidOperationException();
        }

        public class CombineWithTwoErrorablesAndFuncs : ErrorableExtensionTests
        {
            // Known errorable values for testing
            private readonly Errorable<int> _nullErrorable = null;
            private readonly Errorable<int> _firstSuccess = Errorable.Success(4);
            private readonly Errorable<int> _firstFailure = Errorable.Failure<int>("Failure the first");
            private readonly Errorable<int> _secondSuccess = Errorable.Success(2);
            private readonly Errorable<int> _secondFailure = Errorable.Failure<int>("Failure the second");
            private readonly Errorable<int> _thirdSuccess = Errorable.Success(8);
            private readonly Errorable<int> _thirdFailure = Errorable.Failure<int>("Failure the third");

            [Fact]
            public void GivenNullForFirstErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _nullErrorable.Combine(_secondSuccess, _thirdSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullForSecondErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_nullErrorable, _thirdSuccess, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenNullForThirdErrorable_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_secondSuccess, _nullErrorable, WhenSuccessfulDoNothing));
                exception.ParamName.Should().Be("third");
            }

            [Fact]
            public void GivenNullForSuccessAction_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _firstSuccess.Combine(_secondSuccess, _thirdSuccess, (Func<int, int, int, string>)null));
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

                _firstSuccess.Combine(_secondSuccess, _thirdSuccess, WhenSuccessful);

                receivedFu.Should().Be(_firstSuccess.Value);
                receivedBar.Should().Be(_secondSuccess.Value);
                receivedBaz.Should().Be(_thirdSuccess.Value);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndSuccess_ReturnsExpectedResult()
            {
                int WhenSuccessful(int fu, int bar, int baz)
                {
                    return fu * 100 + bar * 10 + baz;
                }

                var result = _firstSuccess.Combine(_secondSuccess, _thirdSuccess, WhenSuccessful);
                result.HasValue.Should().BeTrue();
                result.Value.Should().Be(428);
            }

            [Fact]
            public void GivenSuccessAndSuccessAndFailure_ReturnsExistingErrors()
            {
                var result = _firstSuccess.Combine(_secondSuccess, _thirdFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_thirdFailure.Errors);
            }

            [Fact]
            public void GivenSuccessAndFailureAndSuccess_ReturnsExistingErrors()
            {
                var result = _firstSuccess.Combine(_secondFailure, _thirdSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_secondFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndSuccessAndSuccess_ReturnsExistingErrors()
            {
                var result = _firstFailure.Combine(_secondSuccess, _thirdSuccess, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_firstFailure.Errors);
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_ReturnsCombinedErrors()
            {
                var result = _firstFailure.Combine(_secondFailure, _thirdFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(
                    _firstFailure.Errors.Union(_secondFailure.Errors).Union(_thirdFailure.Errors));
            }

            [Fact]
            public void GivenFailureAndFailureAndFailure_CallsFailureActionWithUniqueErrors()
            {
                var result = _firstFailure.Combine(_firstFailure, _firstFailure, WhenSuccessfullAbort);
                result.Errors.Should().BeEquivalentTo(_firstFailure.Errors);
            }

            // A safe default action for when a test doesn't care about success
            private int WhenSuccessfulDoNothing(int alpht, int beta, int gamma) => 0;

            // A success action that throws an exception to fail a test
            private int WhenSuccessfullAbort(int alkpha, int beta, int gamma) => throw new InvalidOperationException();
        }
    }
}
