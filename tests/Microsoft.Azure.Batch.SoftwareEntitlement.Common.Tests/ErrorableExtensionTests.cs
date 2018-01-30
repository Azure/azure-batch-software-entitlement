using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    /// <summary>
    /// Tests for the extension methods available for <see cref="Errorable{T}"/>
    /// </summary>
    public abstract class ErrorableExtensionTests
    {
        // Known Errorable<T> values for testing
        private readonly Errorable<int> _missingInt = null;
        private readonly Errorable<string> _missingString = null;
        private readonly Errorable<int> _success = Errorable.Success(4);
        private readonly Errorable<int> _failure = Errorable.Failure<int>("failure");
        private readonly Errorable<string> _otherSuccess = Errorable.Success("two");
        private readonly Errorable<string> _otherFailure = Errorable.Failure<string>("other failure");
        private readonly Errorable<int> _yetAnotherSuccess = Errorable.Success(8);
        private readonly Errorable<int> _yetAnotherFailure = Errorable.Failure<int>("yet another failure");

        public class WithReturningTuple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenNullOnLeft_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _missingInt.With(_success));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("left");
            }

            [Fact]
            public void GivenNullOnRight_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _success.With(_missingInt));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("right");
            }

            [Fact]
            public void GivenFailureOnLeft_ReturnsFailureWithSameErrors()
            {
                var result = _failure.With(_success);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenFailureOnRight_ReturnsFailureWithSameErrors()
            {
                var result = _success.With(_failure);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void GivenFailureOnBothSides_ReturnsFailureWithAllErrors()
            {
                var result = _failure.With(_otherFailure);
                result.Errors.Should().Contain(_failure.Errors);
                result.Errors.Should().Contain(_otherFailure.Errors);
            }

            [Fact]
            public void GivenSuccessOnBothSides_ReturnsExpectedTuple()
            {
                var result = _success.With(_otherSuccess);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value));
            }
        }


        public class WithHavingTupleOnLeftReturningTriple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenSuccessOnBothSides_ReturnsExpectedTuple()
            {
                var left = _success.With(_otherSuccess);
                var result = left.With(_yetAnotherSuccess);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value, _yetAnotherSuccess.Value));
            }

            [Fact]
            public void WhenFailureOnLeft_ReturnsExpectedErrors()
            {
                var left = _failure.With(_otherSuccess);
                var result = left.With(_yetAnotherSuccess);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void WhenFailureOnRight_ReturnsExpectedErrors()
            {
                var left = _success.With(_otherSuccess);
                var result = left.With(_yetAnotherFailure);
                result.Errors.Should().Contain(_yetAnotherFailure.Errors);
            }
        }

        public class WithHavingTupleOnRightReturningTriple : ErrorableExtensionTests
        {
            [Fact]
            public void GivenTupleOnRight_ReturnsExpectedValue()
            {
                var right = _otherSuccess.With(_yetAnotherSuccess);
                var result = _success.With(right);
                result.Value.Should().Be((_success.Value, _otherSuccess.Value, _yetAnotherSuccess.Value));
            }

            [Fact]
            public void WhenFailureOnLeft_ReturnsExpectedErrors()
            {
                var right = _otherSuccess.With(_yetAnotherSuccess);
                var result = _failure.With(right);
                result.Errors.Should().Contain(_failure.Errors);
            }

            [Fact]
            public void WhenFailureOnRight_ReturnsExpectedErrors()
            {
                var right = _otherSuccess.With(_yetAnotherFailure);
                var result = _success.With(right);
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

        public class Then
        {
            private readonly Errorable<int> _success = Errorable.Success(42);

            private readonly Errorable<int> _failure = Errorable.Failure<int>("Goodbye cruel world.");

            [Fact]
            public void SuccessThenSuccess_ReturnsExpectedValue()
            {
                var result = _success.Then(num => Errorable.Success(num + 1));
                result.Value.Should().Be(43);
            }

            [Fact]
            public void SuccessThenFailure_HasExpectedErrors()
            {
                var result = _success.Then(num => Errorable.Failure<int>("expected error"));
                result.Errors.Count.Should().Be(1);
                result.Errors.Should().Contain("expected error");
            }

            [Fact]
            public void FailureThenAnything_SecondFunctionNotExecuted()
            {
                var executed = false;
                var result = _failure.Then(num =>
                {
                    executed = true;
                    return Errorable.Success(0);
                });

                result.Errors.Count.Should().Be(1);
                executed.Should().BeFalse();
            }
        }
    }
}
