using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with <see cref="Errorable{T}"/>
    /// </summary>
    public static class ErrorableExtensions
    {
        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values into a single value containing a tuple
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are successful, the output is successful; 
        /// if either input is a failure, the output is a failure. All error messages are preserved.
        /// </remarks>
        /// <typeparam name="TLeft">Type of value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="TRight">Type of value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{TLeft}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{TRight}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either both values or a combined 
        /// set of error messages.</returns>
        public static Errorable<(TLeft Left, TRight Right)> With<TLeft, TRight>(
            this Errorable<TLeft> left,
            Errorable<TRight> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.With(right, (l, r) => (l, r));
        }

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values using the <see cref="Errorable{T}.With{TOther,TResult}"/>
        /// method, which combines their errors if either/both are in a failure state.
        /// </summary>
        /// <remarks>
        /// Intended to be used in LINQ query syntax, with a join clause equating to true. E.g.
        /// <code>
        ///     from left in leftErrorable
        ///     join right in rightErrorable on true equals true
        ///     select (left, right)
        /// </code>
        /// </remarks>
        public static Errorable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this Errorable<TOuter> outer,
            Errorable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => outer.With(inner, resultSelector);

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values using the <see cref="Errorable{T}.With{TOther,TResult}"/>
        /// method, which combines their errors if either/both are in a failure state.
        /// This overload allows an error to be specified if the join fails (if the join criteria evaluate to false).
        /// </summary>
        /// <remarks>
        /// Intended to be used in LINQ query syntax, where the "joined to" <see cref="Errorable{T}"/> is specified as
        /// a Tuple containing the error if the join fails. E.g.
        /// <code>
        ///     from expected in expectedErrorable
        ///     join supplied in (suppliedErrorable, "unexpected value supplied")
        ///         on expected equals supplied
        ///     select GetResult(supplied)
        /// </code>
        /// </remarks>
        public static Errorable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this Errorable<TOuter> outer,
            (Errorable<TInner>, string) inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values using the <see cref="Errorable{T}.With{TOther,TResult}"/>
        /// method, which combines their errors if either/both are in a failure state.
        /// This overload allows an error to be specified if the join fails (if the join criteria evaluate to false),
        /// as well as a custom comparer for the join keys.
        /// </summary>
        public static Errorable<TResult> Join<T, TOther, TKey, TResult>(
            this Errorable<T> errorable,
            (Errorable<TOther> Errorable, string JoinError) joinTo,
            Func<T, TKey> localKeySelector,
            Func<TOther, TKey> otherKeySelector,
            Func<T, TOther, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            if (joinTo.Errorable == null || string.IsNullOrEmpty(joinTo.JoinError))
            {
                throw new ArgumentNullException(nameof(joinTo));
            }

            if (localKeySelector == null)
            {
                throw new ArgumentNullException(nameof(localKeySelector));
            }

            if (otherKeySelector == null)
            {
                throw new ArgumentNullException(nameof(otherKeySelector));
            }

            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return
                from combined in errorable.With(
                    joinTo.Errorable,
                    (local, other) => new
                    {
                        LocalKey = localKeySelector(local),
                        OtherKey = otherKeySelector(other),
                        Result = resultSelector(local, other)
                    })
                // Overloaded 'where' which allows an error to be specified if it evaluates to false.
                where (comparer.Equals(combined.LocalKey, combined.OtherKey), joinTo.JoinError)
                select combined.Result;
        }

        /// <summary>
        /// Converts an <see cref="Errorable{T}"/> to a failure state if it doesn't match the specified
        /// predicate.
        /// </summary>
        /// <remarks>
        /// Can be used in LINQ query syntax, e.g.
        /// <code>
        ///     from value in errorable
        ///     where (value > 10, "Value must be greater than 10")
        ///     select value
        /// </code>
        /// </remarks>
        public static Errorable<TSource> Where<TSource>(
            this Errorable<TSource> source,
            Func<TSource, (bool Result, string Error)> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return source.OnSuccess(t =>
            {
                var result = predicate(t);
                return result.Result ? Errorable.Success(t) : Errorable.Failure<TSource>(result.Error);
            });
        }

        /// <summary>
        /// An alias for <see cref="Errorable{T}.OnSuccess{TNext}(Func{T, TNext})"/>.
        /// </summary>
        /// <remarks>
        /// Allows LINQ query syntax for working with values inside <see cref="Errorable{T}"/>, e.g.
        /// <code>
        ///     from value in errorable
        ///     select value
        /// </code>
        /// </remarks>
        public static Errorable<TResult> Select<TSource, TResult>(
            this Errorable<TSource> source,
            Func<TSource, TResult> selector)
            => source.OnSuccess(selector);

        /// <summary>
        /// An alias for <see cref="Errorable{T}.OnSuccess{TNext}(Func{T, Errorable{TNext}})"/>.
        /// </summary>
        public static Errorable<TResult> SelectMany<TSource, TResult>(
            this Errorable<TSource> source,
            Func<TSource, Errorable<TResult>> selector)
            => source.OnSuccess(selector);

        /// <summary>
        /// Chains calls to <see cref="Errorable{T}.OnSuccess{TNext}(Func{T, Errorable{TNext}})"/>
        /// so that the value of <see cref="source"/> becomes the input to <see cref="otherSelector"/>
        /// and <see cref="resultSelector"/> can combine the values of each.
        /// </summary>
        /// <remarks>
        /// Allows LINQ query syntax for chaining functions on <see cref="Errorable{T}"/>, e.g.
        /// <code>
        ///     from a in errorable
        ///     from b in GetErrorableB(a)
        ///     select b
        /// </code>
        /// </remarks>
        public static Errorable<TResult> SelectMany<TSource, TOther, TResult>(
            this Errorable<TSource> source,
            Func<TSource, Errorable<TOther>> otherSelector,
            Func<TSource, TOther, TResult> resultSelector)
            => source.SelectMany(s => otherSelector(s).OnSuccess(o => resultSelector(s, o)));

        /// <summary>
        /// Pulls a <see cref="Task"/> out from inside an <see cref="Errorable"/> so that the
        /// result is awaitable.
        /// </summary>
        public static Task<Errorable<T>> AsTask<T>(this Errorable<Task<T>> errorable)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            return errorable
                .OnSuccess(async t =>
                {
                    var result = await t.ConfigureAwait(false);
                    return Errorable.Success(result);
                })
                .OnFailure(errors => Task.FromResult(Errorable.Failure<T>(errors)));
        }

        /// <summary>
        /// Convert a collection of <see cref="Errorable{T}"/> into an <see cref="Errorable{IEnumerable{T}}"/>
        /// which contains all the items if they were all successful, or all the errors if any weren't.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Errorable"/> values.</typeparam>
        /// <param name="errorables">Collection of <see cref="Errorable{T}"/> items.</param>
        /// <returns>
        /// An <see cref="Errorable{IEnumerable{T}}"/> containing all the items if they were all successful,
        /// or all the errors if any weren't.
        /// </returns>
        public static Errorable<IEnumerable<T>> Reduce<T>(this IEnumerable<Errorable<T>> errorables)
        {
            return errorables.Aggregate(
                Errorable.Success(Enumerable.Empty<T>()),
                (result, errorable) =>
                    from combined in result.With(errorable)
                    select combined.Left.Append(combined.Right));
        }
    }
}
