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
                var receivedAlpha = 0;
                var receivedBeta = 0;

                void WhenSuccessful(int alpha, int beta)
                {
                    receivedAlpha = alpha;
                    receivedBeta = beta;
                }

                _success.Combine(_otherSuccess, WhenSuccessful, WhenFailureAbort);

                receivedAlpha.Should().Be(_success.Value);
                receivedBeta.Should().Be(_otherSuccess.Value);
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
                Func<int, int, int> combinerFunc = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, combinerFunc));
                exception.ParamName.Should().Be("combinerFunc");
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
                Func<int, int, int, string> combinerFunc = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Combine(_otherSuccess, _yetAnotherSuccess, combinerFunc));
                exception.ParamName.Should().Be("combinerFunc");
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

        public class AndReturningTuple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenNullOnLeft_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _missingInt.And(_success));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("left");
            }

            [Fact]
            public void GivenNullOnRight_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.And(_missingInt));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("right");
            }

            [Fact]
            public void GivenFailureOnLeft_ReturnsFailureWithSameErrors()
            {
                var result = _failure.And(_success);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenFailureOnRight_ReturnsFailureWithSameErrors()
            {
                var result = _success.And(_failure);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenFailureOnBothSides_ReturnsFailureWithAllErrors()
            {
                var result = _failure.And(_otherFailure);
                result.Errors.Should().Contain(_failure.Errors);
                result.Errors.Should().Contain(_otherFailure.Errors);
            }

            [Fact]
            public void GivenSuccessOnBothSides_ReturnsExpectedTuple()
            {
                var result = _success.And(_otherSuccess);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value));
            }
        }


        public class AndWithTupleOnLeftReturningTriple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenSuccessOnBothSides_ReturnsExpectedTuple()
            {
                var left = _success.And(_otherSuccess);
                var result = left.And(_yetAnotherSuccess);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value, _yetAnotherSuccess.Value));
            }

            [Fact]
            public void WhenFailureOnLeft_ReturnsExpectedErrors()
            {
                var left = _failure.And(_otherSuccess);
                var result = left.And(_yetAnotherSuccess);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void WhenFailureOnRight_ReturnsExpectedErrors()
            {
                var left = _success.And(_otherSuccess);
                var result = left.And(_yetAnotherFailure);
                result.Errors.Should().Contain(_yetAnotherFailure.Errors);
            }
        }

        public class AndWithTupleOnRightReturningTriple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenTupleOnRight_ReturnsExpectedValue()
            {
                var right = _otherSuccess.And(_yetAnotherSuccess);
                var result = _success.And(right);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value, _yetAnotherSuccess.Value));
            }

            [Fact]
            public void WhenFailureOnLeft_ReturnsExpectedErrors()
            {
                var right = _otherSuccess.And(_yetAnotherSuccess);
                var result = _failure.And(right);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void WhenFailureOnRight_ReturnsExpectedErrors()
            {
                var right = _otherSuccess.And(_yetAnotherFailure);
                var result = _success.And(right);
                result.Errors.Should().Contain(_yetAnotherFailure.Errors);
            }
        }

        public class DoWithTuple
        {
            private int _integer;
            private string _string;
            private List<string> _errors;

            private readonly Errorable<(int, string)> _success = Errorable.Success((42, "hello"));

            private readonly Errorable<(int, string)> _failure = Errorable.Failure<(int, string)>("Goodbye cruel world.");

            [Fact]
            public void WhenSuccess_CallsCorrectAction()
            {
                _success.Do(WhenSuccessful, WhenFailure);
                _integer.Should().Be(42);
                _string.Should().Be("hello");
            }

            [Fact]
            public void WhenFailure_CallsCorrectAction()
            {
                _failure.Do(WhenSuccessful, WhenFailure);
                _errors.Should().Contain("Goodbye cruel world.");
            }

            [Fact]
            public void WhenNullErrorable_ThrowsExpectedException()
            {
                Errorable<(int, string)> errorable = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => errorable.Do(WhenSuccessful, WhenFailure));
                exception.ParamName.Should().Be("errorable");
            }

            [Fact]
            public void WhenNullSuccessAction_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Do(null, WhenFailure));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void WhenNullFailureAction_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Do(WhenSuccessful, null));
                exception.ParamName.Should().Be("whenFailure");
            }

            private void WhenSuccessful(int i, string s)
            {
                _integer = i;
                _string = s;
            }

            private void WhenFailure(IEnumerable<string> errors)
            {
                _errors = errors.ToList();
            }
        }

        public class DoWithTriple
        {
            private int _integer;
            private string _string;
            private bool _bool;

            private List<string> _errors;

            private readonly Errorable<(int, string, bool)> _success =
                Errorable.Success((42, "hello", true));

            private readonly Errorable<(int, string, bool)> _failure =
                Errorable.Failure<(int, string, bool)>("Goodbye cruel world.");

            [Fact]
            public void WhenSuccess_CallsCorrectAction()
            {
                _success.Do(WhenSuccessful, WhenFailure);
                _integer.Should().Be(42);
                _string.Should().Be("hello");
                _bool.Should().BeTrue();
            }

            [Fact]
            public void WhenFailure_CallsCorrectAction()
            {
                _failure.Do(WhenSuccessful, WhenFailure);
                _errors.Should().Contain("Goodbye cruel world.");
            }

            [Fact]
            public void WhenNullErrorable_ThrowsExpectedException()
            {
                Errorable<(int, string, bool)> errorable = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => errorable.Do(WhenSuccessful, WhenFailure));
                exception.ParamName.Should().Be("errorable");
            }

            [Fact]
            public void WhenNullSuccessAction_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Do(null, WhenFailure));
                exception.ParamName.Should().Be("whenSuccessful");
            }

            [Fact]
            public void WhenNullFailureAction_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Do(WhenSuccessful, null));
                exception.ParamName.Should().Be("whenFailure");
            }

            private void WhenSuccessful(int i, string s, bool b)
            {
                _integer = i;
                _string = s;
                _bool = b;
            }

            private void WhenFailure(IEnumerable<string> errors)
            {
                _errors = errors.ToList();
            }
        }

        public class MapOfTuple
        {
            private readonly Errorable<(int, string)> _success = Errorable.Success((42, "hello"));

            private readonly Errorable<(int, string)> _failure = Errorable.Failure<(int, string)>("Goodbye cruel world.");

            [Fact]
            public void GivenTransformationOfSuccess_ReturnsExpectedValue()
            {
                var result =_success.Map(Transform);
                result.Value.Should().Be(42);
            }

            [Fact]
            public void GivenTransformationOfFailure_HasExpectedErrors()
            {
                var result = _failure.Map(Transform);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenNullErrorable_ThrowsExpectedException()
            {
                Errorable<(int, string)> errorable = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => errorable.Map(Transform));
                exception.ParamName.Should().Be("errorable");
            }

            [Fact]
            public void GivenNullSuccessfulFunc_ThrowsExpectedException()
            {
                Func<int, string, int> func = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Map(func));
                exception.ParamName.Should().Be("transform");
            }

            private int Transform(int i, string s)
            {
                return 42;
            }
        }

        public class MapOfTriple
        {
            private readonly Errorable<(int, string, bool)> _success = Errorable.Success((42, "hello", true));

            private readonly Errorable<(int, string, bool)> _failure = Errorable.Failure<(int, string, bool)>("Goodbye cruel world.");

            [Fact]
            public void GivenTransformationOfSuccess_ReturnsExpectedValue()
            {
                var result = _success.Map(Transform);
                result.Value.Should().Be(42);
            }

            [Fact]
            public void GivenTransformationOfFailure_HasExpectedErrors()
            {
                var result = _failure.Map(Transform);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenNullErrorable_ThrowsExpectedException()
            {
                Errorable<(int, string, bool)> errorable = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => errorable.Map(Transform));
                exception.ParamName.Should().Be("errorable");
            }

            [Fact]
            public void GivenNullSuccessfulFunc_ThrowsExpectedException()
            {
                Func<int, string, bool, int> func = null;
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.Map(func));
                exception.ParamName.Should().Be("transform");
            }

            private int Transform(int i, string s, bool b)
            {
                return 42;
            }
        }
    }
}
