using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class ResultExtensionTests
    {
        protected const int DefaultOkInt = 12;
        protected const int DefaultOtherOkInt = 42;

        protected const string DefaultErrorString = "error";
        protected const string DefaultOtherErrorString = "other";

        protected static readonly Result<int, CombinableString> OkIntResult = new Result<int, CombinableString>(DefaultOkInt);
        protected static readonly Result<int, CombinableString> OtherOkIntResult = new Result<int, CombinableString>(DefaultOtherOkInt);

        protected static readonly Result<int, CombinableString> ErrorIntResult = new Result<int, CombinableString>(DefaultErrorString);
        protected static readonly Result<int, CombinableString> OtherErrorIntResult =
            new Result<int, CombinableString>(DefaultOtherErrorString);

        protected static readonly Result<int, CombinableString> MissingIntResult = null;

        protected class CombinableString : ICombinable<CombinableString>
        {
            public CombinableString(string value)
            {
                Content = value;
            }

            public CombinableString Combine(CombinableString combinable)
                => new CombinableString($"{Content}, {combinable.Content}");

            public string Content { get; }

            public static implicit operator CombinableString(string value) =>
                new CombinableString(value);
        }

        public class WithReturningTuple : ResultExtensionTests
        {
            [Fact]
            public void GivenNullFirst_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => MissingIntResult.With(OkIntResult));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("first");
            }

            [Fact]
            public void GivenNullSecond_ThrowsExpectedException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => OkIntResult.With(MissingIntResult));
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("second");
            }

            [Fact]
            public void GivenErrorFirst_ReturnsSameErrors()
            {
                var result = ErrorIntResult.With(OkIntResult);
                result.GetError().Content.Should().Be(DefaultErrorString);
            }

            [Fact]
            public void GivenErrorSecond_ReturnsSameErrors()
            {
                var result = OkIntResult.With(ErrorIntResult);
                result.GetError().Content.Should().Be(DefaultErrorString);
            }

            [Fact]
            public void GivenErrorOnBothSides_ReturnsAllErrors()
            {
                var result = ErrorIntResult.With(OtherErrorIntResult);
                result.GetError().Content.Should().Be($"{DefaultErrorString}, {DefaultOtherErrorString}");
            }

            [Fact]
            public void GivenSuccessOnBothSides_ReturnsExpectedTuple()
            {
                var result = OkIntResult.With(OtherOkIntResult);
                result.GetOk().Should().Be((DefaultOkInt, DefaultOtherOkInt));
            }
        }

        public class JoinWithoutKey : ResultExtensionTests
        {
            private int _invocationCount;

            [Fact]
            public void GivenNullOuter_ThrowsExpectedException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    from outer in MissingIntResult
                    join inner in OkIntResult on true equals true
                    select outer + inner);
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("outer");
            }

            [Fact]
            public void GivenNullInner_ThrowsExpectedException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    from outer in OkIntResult
                    join inner in MissingIntResult on true equals true
                    select outer + inner);
                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("inner");
            }

            [Fact]
            public void IgnoresJoinCriteria()
            {
                var result =
                    from outer in OkIntResult
                    join inner in OkIntResult on 1 equals 2
                    select outer + inner;

                result.IsError().Should().BeFalse();
                result.GetOk().Should().Be(DefaultOkInt * 2);
            }

            [Fact]
            public void DoesNotExecuteJoinKeyFunctions()
            {
                int GetJoinKey(int i)
                {
                    _invocationCount++;
                    return i;
                }

                var result = OkIntResult.Join(
                    OkIntResult,
                    GetJoinKey,
                    GetJoinKey,
                    (outer, inner) => outer + inner);

                result.IsError().Should().BeFalse();
                _invocationCount.Should().Be(0);
            }

            [Fact]
            public void JoinKeyFunctionsCanBeNull()
            {
                var result = OkIntResult.Join(
                    OkIntResult,
                    null as Func<int, int>,
                    null as Func<int, int>,
                    (outer, inner) => outer + inner);

                result.IsError().Should().BeFalse();
            }

            [Fact]
            public void OnOuterError_ReturnsSameError()
            {
                var result =
                    from outer in ErrorIntResult
                    join inner in OkIntResult on true equals true
                    select outer + inner;

                result.GetError().Content.Should().Be(DefaultErrorString);
            }

            [Fact]
            public void OnOuterError_ResultSelectorIsNotInvoked()
            {
                string SelectResult(int i, int j)
                {
                    _invocationCount++;
                    return $"{i},{j}";
                }

                var result =
                    from outer in ErrorIntResult
                    join inner in OkIntResult on true equals true
                    select SelectResult(outer, inner);

                result.IsError().Should().BeTrue();
                _invocationCount.Should().Be(0);
            }

            [Fact]
            public void OnInnerError_ReturnsSameError()
            {
                var result =
                    from outer in OkIntResult
                    join inner in ErrorIntResult on true equals true
                    select outer + inner;

                result.GetError().Content.Should().Be(DefaultErrorString);
            }

            [Fact]
            public void OnInnerError_ResultSelectorIsNotInvoked()
            {
                string SelectResult(int i, int j)
                {
                    _invocationCount++;
                    return $"{i},{j}";
                }

                var result =
                    from outer in OkIntResult
                    join inner in ErrorIntResult on true equals true
                    select SelectResult(outer, inner);

                result.IsError().Should().BeTrue();
                _invocationCount.Should().Be(0);
            }

            [Fact]
            public void OnErrorOnBothSides_ReturnsAllErrors()
            {
                var result =
                    from outer in ErrorIntResult
                    join inner in OtherErrorIntResult on true equals true
                    select $"{outer} {inner}";

                result.GetError().Content.Should().Be($"{DefaultErrorString}, {DefaultOtherErrorString}");
            }

            [Fact]
            public void OnErrorOnBothSides_ResultSelectorIsNotInvoked()
            {
                string SelectResult(int i, int j)
                {
                    _invocationCount++;
                    return $"{i},{j}";
                }

                var result =
                    from outer in ErrorIntResult
                    join inner in OtherErrorIntResult on true equals true
                    select SelectResult(outer, inner);

                result.IsError().Should().BeTrue();
                _invocationCount.Should().Be(0);
            }

            [Fact]
            public void OnSuccessOnBothSides_ReturnsExpectedResult()
            {
                var result =
                    from outer in OkIntResult
                    join inner in OtherOkIntResult on true equals true
                    select outer + inner;

                result.GetOk().Should().Be(DefaultOkInt + DefaultOtherOkInt);
            }

            [Fact]
            public void OnSuccessOnBothSides_ResultSelectorIsInvokedOnce()
            {
                string SelectResult(int i, int j)
                {
                    _invocationCount++;
                    return $"{i},{j}";
                }

                var result =
                    from outer in OkIntResult
                    join inner in OtherOkIntResult on true equals true
                    select SelectResult(outer, inner);

                result.IsError().Should().BeFalse();
                _invocationCount.Should().Be(1);
            }
        }

        public class JoinWithCriteria : ResultExtensionTests
        {
            [Fact]
            public void OnJoinFailure_AddsError()
            {
                var result =
                    from outer in OkIntResult
                    join inner in (OtherOkIntResult, "join failed") on 1 equals 0
                    select $"{outer} {inner}";

                result.GetError().Content.Should().Be("join failed");
            }
        }

        public class Where : ResultExtensionTests
        {
            [Fact]
            public void GivenNullSource_ThrowsExpectedException()
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => MissingIntResult.Where(i => true.AsPredicateResult(new CombinableString("unused error"))));

                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("source");
            }

            [Fact]
            public void GivenNullPredicate_ThrowsExpectedException()
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => OkIntResult.Where(null));

                exception.Should().NotBeNull();
                exception.ParamName.Should().Be("predicate");
            }

            [Fact]
            public void WhenPredicateIsTrue_ReturnsResult()
            {
                var result = OkIntResult.Where(i => true.AsPredicateResult(new CombinableString("unused error")));
                result.GetOk().Should().Be(DefaultOkInt);
            }

            [Fact]
            public void WhenPredicateIsFalse_UsesSpecifiedError()
            {
                var predicateError = new CombinableString("predicate error");
                var result = OkIntResult.Where(i => false.AsPredicateResult(predicateError));
                result.GetError().Content.Should().Be(predicateError.Content);
            }

            [Fact]
            public void CanUseQuerySyntax()
            {
                var result =
                    from r in OkIntResult
                    where true.AsPredicateResult(new CombinableString("unused error"))
                    select r;

                result.GetOk().Should().Be(DefaultOkInt);
            }
        }

        public class MergeSameType : ResultExtensionTests
        {
            [Fact]
            public void WhenOk_ReturnsOk()
            {
                var right = new Result<int, int>(ok: 7);
                var result = right.Merge();
                result.Should().Be(7);
            }

            [Fact]
            public void WhenError_ReturnsError()
            {
                var errorResult = new Result<int, int>(error: 47);
                var result = errorResult.Merge();
                result.Should().Be(47);
            }
        }

        public class MergeDifferentTypes : ResultExtensionTests
        {
            [Fact]
            public void WhenOk_ReturnsOk()
            {
                var result = OkIntResult.Merge(e => -1);
                result.Should().Be(DefaultOkInt);
            }

            [Fact]
            public void WhenError_ReturnsConvertedError()
            {
                var result = ErrorIntResult.Merge(i => -1);
                result.Should().Be(-1);
            }
        }

        public class OnErrorWithAction : ResultExtensionTests
        {
            [Fact]
            public void WhenError_CallsSuppliedAction()
            {
                bool invoked = false;
                ErrorIntResult.OnError(i =>
                {
                    invoked = true;
                });
                invoked.Should().Be(true);
            }

            [Fact]
            public void WhenOk_DoesNotCallSuppliedAction()
            {
                bool invoked = false;
                OkIntResult.OnError(i =>
                {
                    invoked = true;
                });
                invoked.Should().Be(false);
            }
        }

        public class OnErrorWithFunc : ResultExtensionTests
        {
            [Fact]
            public void WhenError_CallsSuppliedFunc()
            {
                bool invoked = false;
                var result = ErrorIntResult.OnError(error =>
                {
                    invoked = true;
                    return error.Content + "!";
                });

                invoked.Should().Be(true);
                result.GetError().Should().Be(DefaultErrorString + "!");
            }

            [Fact]
            public void WhenOk_DoesNotCallSuppliedFunc()
            {
                bool invoked = false;
                var result = OkIntResult.OnError(error =>
                {
                    invoked = true;
                    return error + "!";
                });

                invoked.Should().Be(false);
                result.IsOk().Should().BeTrue();
            }
        }

        public class OnOkWithAction : ResultExtensionTests
        {
            [Fact]
            public void WhenError_DoesNotCallSuppliedAction()
            {
                bool invoked = false;
                ErrorIntResult.OnOk(i =>
                {
                    invoked = true;
                });
                invoked.Should().Be(false);
            }

            [Fact]
            public void WhenOk_CallsSuppliedAction()
            {
                bool invoked = false;
                OkIntResult.OnOk(i =>
                {
                    invoked = true;
                });
                invoked.Should().Be(true);
            }
        }

        public class OnOkWithFunc : ResultExtensionTests
        {
            [Fact]
            public void WhenError_DoesNotCallSuppliedFunc()
            {
                bool invoked = false;
                var result = ErrorIntResult.OnOk(i =>
                {
                    invoked = true;
                    return i + 1;
                });

                invoked.Should().Be(false);
                result.IsError().Should().BeTrue();
            }

            [Fact]
            public void WhenOk_CallsSuppliedFunc()
            {
                bool invoked = false;
                var result = OkIntResult.OnOk(i =>
                {
                    invoked = true;
                    return i + 1;
                });

                invoked.Should().Be(true);
                result.IsOk().Should().BeTrue();
                result.GetOk().Should().Be(DefaultOkInt + 1);
            }
        }

        public class OnOkWithResultFunc : ResultExtensionTests
        {
            [Fact]
            public void WhenError_DoesNotCallSuppliedFunc()
            {
                bool invoked = false;
                var result = ErrorIntResult.OnOk(i =>
                {
                    invoked = true;
                    return new Result<int, CombinableString>(i + 1);
                });

                invoked.Should().Be(false);
                result.IsError().Should().BeTrue();
            }

            [Fact]
            public void WhenOk_CallsSuppliedFunc()
            {
                bool invoked = false;
                var result = OkIntResult.OnOk(i =>
                {
                    invoked = true;
                    return new Result<int, CombinableString>(i + 1);
                });

                invoked.Should().Be(true);
                result.IsOk().Should().BeTrue();
                result.GetOk().Should().Be(DefaultOkInt + 1);
            }
        }

        public class SelectMethod : ResultExtensionTests
        {
            [Fact]
            public void CanUseQuerySyntax()
            {
                var result =
                    from r in OkIntResult
                    select r;

                result.GetOk().Should().Be(DefaultOkInt);
            }
        }

        public class SelectAllMethod : ResultExtensionTests
        {
            [Fact]
            public void CanUseQuerySyntax()
            {
                var result =
                    from r in OkIntResult
                    from rr in new Result<int, CombinableString>(r + 1)
                    select rr;

                result.GetOk().Should().Be(DefaultOkInt + 1);
            }
        }
    }
}
