using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Combine two <see cref="Result{TOk,TError}"/> values into a single value containing a value
        /// constructed from the two OK states of each <see cref="Result{TOk,TError}"/>.
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are OK, the output is OK;
        /// if either input is an error, the output is an error.
        /// </remarks>
        /// <returns></returns>
        public static Result<TResultOk, TError> With<TLocalOk, TError, TOtherOk, TResultOk>(
            this Result<TLocalOk, TError> result,
            Result<TOtherOk, TError> otherResult,
            Func<TLocalOk, TOtherOk, TResultOk> okCombiner)
            where TError : ICombinable<TError>
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (otherResult == null) throw new ArgumentNullException(nameof(otherResult));
            if (okCombiner == null) throw new ArgumentNullException(nameof(okCombiner));

            return result.Match(
                fromOk: localOk => otherResult.Match(
                    fromOk: otherOk => new Result<TResultOk, TError>(okCombiner(localOk, otherOk)),
                    fromError: otherError => new Result<TResultOk, TError>(otherError)
                    ),
                fromError: localError => otherResult.Match(
                    fromOk: otherOk => new Result<TResultOk, TError>(localError),
                    fromError: otherError => new Result<TResultOk, TError>(localError.Combine(otherError))
                    )
                );
        }

        /// <summary>
        /// Combine two <see cref="Result{TOk,TError}"/> values into a single value containing a tuple
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are OK, the output is OK;
        /// if either input is an error, the output is an error.
        /// </remarks>
        /// <returns></returns>
        public static Result<(TFirstOk First, TSecondOk Second), TError> With<TFirstOk, TError, TSecondOk>(
            this Result<TFirstOk, TError> first,
            Result<TSecondOk, TError> second)
            where TError : ICombinable<TError>
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first.With(second, (v1, v2) => (v1, v2));
        }

        /// <summary>
        /// Combine two <see cref="Result{TOk,TError}"/> values using the <see cref="With{TLocalOk,TError,TOtherOk,TResultOk}"/>
        /// method, which combines their errors if either/both are in an error state.
        /// </summary>
        /// <remarks>
        /// Intended to be used in LINQ query syntax, with a join clause equating to true. E.g.
        /// <code>
        ///     from v1 in result1
        ///     join v2 in result2 on true equals true
        ///     select (v1, v2)
        /// </code>
        /// </remarks>
        public static Result<TResult, TError> Join<TOuter, TError, TInner, TKey, TResult>(
            this Result<TOuter, TError> outer,
            Result<TInner, TError> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TError : ICombinable<TError>
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));

            return outer.With(inner, resultSelector);
        }

        /// <summary>
        /// Combine two <see cref="Result{TOk,TError}"/> values using the <see cref="With{TLocalOk,TError,TOtherOk,TResultOk}"/>
        /// method, which combines their errors if either/both are in an error state.
        /// This overload allows an error to be specified if the join fails (if the join criteria evaluate to false).
        /// </summary>
        /// <remarks>
        /// Intended to be used in LINQ query syntax, where the "joined to" <see cref="Result{TOk,TError}"/> is specified as
        /// a Tuple containing the error if the join fails. E.g.
        /// <code>
        ///     from expected in expectedResult
        ///     join supplied in (suppliedResult, "unexpected value supplied")
        ///         on expected equals supplied
        ///     select GetResult(supplied)
        /// </code>
        /// </remarks>
        public static Result<TResult, TError> Join<TOuter, TError, TInner, TKey, TResult>(
            this Result<TOuter, TError> outer,
            (Result<TInner, TError> Result, TError JoinError) inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            where TError : ICombinable<TError> => outer.Join(
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default);

        /// <summary>
        /// Combine two <see cref="Result{TOk,TError}"/> values using the <see cref="With{TLocalOk,TError,TOtherOk,TResultOk}"/>
        /// method, which combines their errors if either/both are in an error state.
        /// This overload allows an error to be specified if the join fails (if the join criteria evaluate to false),
        /// as well as a custom comparer for the join keys.
        /// </summary>
        public static Result<TResult, TError> Join<T, TError, TOther, TKey, TResult>(
            this Result<T, TError> outer,
            (Result<TOther, TError> Result, TError JoinError) joinTo,
            Func<T, TKey> localKeySelector,
            Func<TOther, TKey> otherKeySelector,
            Func<T, TOther, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
            where TError : ICombinable<TError>
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (joinTo.Result == null || EqualityComparer<TError>.Default.Equals(joinTo.JoinError, default))
                throw new ArgumentNullException(nameof(joinTo));
            if (localKeySelector == null) throw new ArgumentNullException(nameof(localKeySelector));
            if (otherKeySelector == null) throw new ArgumentNullException(nameof(otherKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            return
                from combined in outer.With(
                    joinTo.Result,
                    (local, other) => new
                    {
                        LocalKey = localKeySelector(local),
                        OtherKey = otherKeySelector(other),
                        Result = resultSelector(local, other)
                    })
                let keysMatch = comparer.Equals(combined.LocalKey, combined.OtherKey)
                // Overloaded 'where' which allows an error to be specified if it evaluates to false.
                where keysMatch.AsPredicateResult(joinTo.JoinError)
                select combined.Result;
        }

        /// <summary>
        /// Converts an <see cref="Result{TOk,TError}"/> to an error state if it doesn't match the specified
        /// predicate.
        /// </summary>
        /// <remarks>
        /// Can be used in LINQ query syntax, e.g.
        /// <code>
        ///     from val in result
        ///     where (val > 10).AsPredicateResult(GetError)
        ///     select val
        /// </code>
        /// </remarks>
        public static Result<TOk, TError> Where<TOk, TError>(
            this Result<TOk, TError> source,
            Func<TOk, PredicateResult<TError>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            Result<TOk, TError> Filter(TOk ok) =>
                predicate(ok).Match(
                    onSuccess: () => new Result<TOk, TError>(ok),
                    onFailure: error => new Result<TOk, TError>(error));

            return source.OnOk(Filter);
        }

        /// <summary>
        /// Combines the error and OK values when they both have the same type
        /// (i.e. takes whichever has the value).
        /// </summary>
        public static T Merge<T>(this Result<T, T> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.Match(ok => ok, error => error);
        }

        /// <summary>
        /// Combines the error and OK values. If the <see cref="Result{TLeft,TRight}"/>
        /// has the left value, the specified function is used to convert it to the right
        /// type.
        /// </summary>
        public static TOk Merge<TOk, TError>(
            this Result<TOk, TError> result,
            Func<TError, TOk> fromError)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (fromError == null) throw new ArgumentNullException(nameof(fromError));

            return result.OnError(fromError).Merge();
        }

        /// <summary>
        /// Take an action if we have an error value
        /// </summary>
        /// <param name="result"></param>
        /// <param name="action">Action to take when we have an error value.</param>
        public static void OnError<TOk, TError>(
            this Result<TOk, TError> result,
            Action<TError> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (action == null) throw new ArgumentNullException(nameof(action));

            result.Match(okAction: _ => { }, errorAction: action);
        }

        /// <summary>
        /// Returns the result of a function if we have an error value
        /// </summary>
        /// <param name="result"></param>
        /// <param name="operation">Function to call if we have an error value</param>
        /// <returns></returns>
        public static Result<TOk, TNextError> OnError<TOk, TError, TNextError>(
            this Result<TOk, TError> result,
            Func<TError, TNextError> operation)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            return result.Match(
                fromOk: ok => new Result<TOk, TNextError>(ok),
                fromError: error => new Result<TOk, TNextError>(operation(error))
                );
        }

        /// <summary>
        /// Take an action if we have an OK value
        /// </summary>
        /// <param name="result"></param>
        /// <param name="action">Action to take when we have an OK value.</param>
        public static void OnOk<TOk, TError>(
            this Result<TOk, TError> result,
            Action<TOk> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (action == null) throw new ArgumentNullException(nameof(action));

            result.Match(okAction: action, errorAction: _ => { });
        }

        /// <summary>
        /// Executes a function returning a <see cref="TNextOk"/> conditionally, depending
        /// on the result of this <see cref="Result{TOk,TError}"/> instance.
        /// </summary>
        /// <typeparam name="TOk"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <typeparam name="TNextOk">The return type of <paramref name="operation"/></typeparam>
        /// <param name="result"></param>
        /// <param name="operation">A function to execute on the value of this instance if it
        /// is successful</param>
        /// <returns>
        /// An <see cref="Errorable{TNextOk}"/> containing the result of executing <paramref name="operation"/>
        /// if the input was successful, or the errors from this instance otherwise.
        /// </returns>
        public static Result<TNextOk, TError> OnOk<TOk, TError, TNextOk>(
            this Result<TOk, TError> result,
            Func<TOk, TNextOk> operation)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            return result.Match(
                fromOk: ok => new Result<TNextOk, TError>(operation(ok)),
                fromError: error => new Result<TNextOk, TError>(error)
                );
        }

        /// <summary>
        /// Executes a function returning a <see cref="Result{TLeft,TNextRight}"/> conditionally, depending
        /// on the result of this <see cref="Result{TLeft,TRight}"/> instance.
        /// </summary>
        /// <typeparam name="TOk"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <typeparam name="TNextOk">The return type of <paramref name="operation"/></typeparam>
        /// <param name="result"></param>
        /// <param name="operation">A function to execute on the value of this instance if it
        /// is successful</param>
        /// <returns>
        /// An <see cref="Result{TNextOk,TError}"/> containing the result of executing <paramref name="operation"/>
        /// if the input was successful, or the errors from this instance otherwise.
        /// </returns>
        public static Result<TNextOk, TError> OnOk<TOk, TError, TNextOk>(
            this Result<TOk, TError> result,
            Func<TOk, Result<TNextOk, TError>> operation)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            return result.Match(
                fromOk: operation,
                fromError: error => new Result<TNextOk, TError>(error)
                );
        }

        /// <summary>
        /// An alias for <see cref="OnOk{TOk,TError}"/>.
        /// </summary>
        /// <remarks>
        /// Allows LINQ query syntax for working with OK values inside <see cref="Result{TOk,TError}"/>, e.g.
        /// <code>
        ///     from val in result
        ///     select val
        /// </code>
        /// </remarks>
        public static Result<TResultOk, TError> Select<TSourceOk, TError, TResultOk>(
            this Result<TSourceOk, TError> source,
            Func<TSourceOk, TResultOk> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return source.OnOk(selector);
        }

        /// <summary>
        /// Chains calls to <see cref="OnOk{TOk,TError}"/>
        /// so that the value of <see cref="source"/> becomes the input to <see cref="otherSelector"/>
        /// and <see cref="resultSelector"/> can combine the values of each.
        /// </summary>
        /// <remarks>
        /// Allows LINQ query syntax for chaining functions on <see cref="Result{TOk,TError}"/>, e.g.
        /// <code>
        ///     from val1 in result1
        ///     from val2 in GetResult2(val1)
        ///     select val1 + val2
        /// </code>
        /// </remarks>
        public static Result<TResultOk, TError> SelectMany<TSourceOk, TError, TOtherOk, TResultOk>(
            this Result<TSourceOk, TError> source,
            Func<TSourceOk, Result<TOtherOk, TError>> otherSelector,
            Func<TSourceOk, TOtherOk, TResultOk> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (otherSelector == null) throw new ArgumentNullException(nameof(otherSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.OnOk(s => otherSelector(s).OnOk(o => resultSelector(s, o)));
        }

        /// <summary>
        /// Converts a <see cref="bool"/> to a <see cref="PredicateResult{TFailure}"/> calling
        /// the specified function to create the <typeparam name="TLeft"></typeparam> value
        /// if it's <value>false</value>.
        /// </summary>
        public static PredicateResult<TLeft> AsPredicateResult<TLeft>(
            this bool result,
            Func<TLeft> whenLeft) =>
            result ? PredicateResult<TLeft>.Success() : PredicateResult<TLeft>.Failure(whenLeft());

        /// <summary>
        /// Converts a <see cref="bool"/> to a <see cref="PredicateResult{TFailure}"/> calling
        /// the specified <typeparam name="TError"></typeparam> value if it's <value>false</value>.
        /// </summary>
        public static PredicateResult<TError> AsPredicateResult<TError>(
            this bool result,
            TError whenError) =>
            result.AsPredicateResult(() => whenError);

        /// <summary>
        /// Converts a <see cref="bool"/> to a <see cref="PredicateResult{TFailure}"/> calling
        /// the specified function to create the <typeparam name="TError"></typeparam> value
        /// if it's <value>true</value>.
        /// </summary>
        public static PredicateResult<TError> AsPredicateFailure<TError>(
            this bool result,
            Func<TError> whenError) =>
            (!result).AsPredicateResult(whenError);

        /// <summary>
        /// Pulls a <see cref="Task"/> out from inside an <see cref="Result{TOk,TError}"/> so that the
        /// result is awaitable.
        /// </summary>
        public static Task<Result<TOk, TError>> AsTask<TOk, TError>(this Result<Task<TOk>, TError> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result
                .OnOk(async t =>
                {
                    var ok = await t.ConfigureAwait(false);
                    return new Result<TOk, TError>(ok);
                })
                .Merge(error => Task.FromResult(new Result<TOk, TError>(error)));
        }

        /// <summary>
        /// Converts a <see cref="Result{TOk,ErrorCollection}"/> to an equivalent <see cref="Errorable{TOk}"/>
        /// </summary>
        public static Errorable<TOk> AsErrorable<TOk>(this Result<TOk, ErrorCollection> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.Match(
                fromOk: Errorable<TOk>.CreateSuccess,
                fromError: Errorable<TOk>.CreateFailure
            );
        }

        /// <summary>
        /// Convert a collection of <see cref="Result{TOk,TError}"/> into an <see cref="Result{TOk,TError}"/>
        /// which contains all the items if they were all OK, or all the errors if there were any.
        /// </summary>
        /// <param name="results">Collection of <see cref="Result{TOk,TError}"/> items.</param>
        /// <returns>
        /// A <see cref="Result{TOk,TError}"/> containing all the items if they were all successful,
        /// or all the errors if any weren't.
        /// </returns>
        public static Result<IEnumerable<TOk>, TError> Reduce<TOk, TError>(
            this IEnumerable<Result<TOk, TError>> results)
            where TError : ICombinable<TError>
        {
            return results.Aggregate(
                new Result<IEnumerable<TOk>, TError>(Enumerable.Empty<TOk>()),
                (result, either) =>
                    from combined in result.With(either)
                    select combined.First.Append(combined.Second));
        }
    }
}
