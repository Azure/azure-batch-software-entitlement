using System;
using System.Collections.Generic;

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
        /// <typeparam name="A">Type of value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="B">Type of value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{T}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{T}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either both values or a combined 
        /// set of error messages.</returns>
        public static Errorable<(A, B)> And<A, B>(this Errorable<A> left, Errorable<B> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.Match(
                whenSuccessful: leftValue => right.Match(
                    whenSuccessful: rightValue => Errorable.Success((leftValue, rightValue)),
                    whenFailure: errors => Errorable.Failure<(A, B)>(errors)),
                whenFailure: leftErrors => right.Match(
                    whenSuccessful: rightValue => Errorable.Failure<(A, B)>(leftErrors),
                    whenFailure: errors => Errorable.Failure<(A, B)>(leftErrors).AddErrors(errors)));
        }

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values into a single value containing a tuple 
        /// of three values
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are successful, the output is successful; 
        /// if either input is a failure, the output is a failure. All error messages are preserved.
        /// </remarks>
        /// <typeparam name="A">Type of the first value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="B">Type of the second value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="C">Type of value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{T}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{T}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either all three values or a 
        /// combined set of error messages.</returns>
        public static Errorable<(A, B, C)> And<A, B, C>(this Errorable<(A, B)> left, Errorable<C> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.Match(
                whenSuccessful: leftValue => right.Match(
                    whenSuccessful: rightValue => Errorable.Success((leftValue.Item1, leftValue.Item2, rightValue)),
                    whenFailure: errors => Errorable.Failure<(A, B, C)>(errors)),
                whenFailure: leftErrors => right.Match(
                    whenSuccessful: rightValue => Errorable.Failure<(A, B, C)>(leftErrors),
                    whenFailure: errors => Errorable.Failure<(A, B, C)>(leftErrors).AddErrors(errors)));
        }

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values into a single value containing a tuple 
        /// of three values
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are successful, the output is successful; 
        /// if either input is a failure, the output is a failure. All error messages are preserved.
        /// </remarks>
        /// <typeparam name="A">Type of the value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="B">Type of the first value held by <paramref name="right"/>.</typeparam>
        /// <typeparam name="C">Type of the second value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{T}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{T}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either all three values or a 
        /// combined set of error messages.</returns>
        public static Errorable<(A, B, C)> And<A, B, C>(this Errorable<A> left, Errorable<(B, C)> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.Match(
                whenSuccessful: leftValue => right.Match(
                    whenSuccessful: rightValue => Errorable.Success((leftValue, rightValue.Item1, rightValue.Item2)),
                    whenFailure: errors => Errorable.Failure<(A, B, C)>(errors)),
                whenFailure: leftErrors => right.Match(
                    whenSuccessful: rightValue => Errorable.Failure<(A, B, C)>(leftErrors),
                    whenFailure: errors => Errorable.Failure<(A, B, C)>(leftErrors).AddErrors(errors)));
        }

        public static void Do<A, B>(
            this Errorable<(A, B)> errorable,
            Action<A, B> whenSuccessful,
            Action<IEnumerable<string>> whenFailure)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (whenFailure == null)
            {
                throw new ArgumentNullException(nameof(whenFailure));
            }

            errorable.Match(t => whenSuccessful(t.Item1, t.Item2), whenFailure);
        }

        public static void Do<A, B, C>(
            this Errorable<(A, B, C)> errorable,
            Action<A, B, C> whenSuccessful,
            Action<IEnumerable<string>> whenFailure)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (whenFailure == null)
            {
                throw new ArgumentNullException(nameof(whenFailure));
            }

            errorable.Match(t => whenSuccessful(t.Item1, t.Item2, t.Item3), whenFailure);
        }

        public static Errorable<R> Map<A, B, R>(
            this Errorable<(A, B)> errorable,
            Func<A, B, R> transform)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            return errorable.Match(
                t => Errorable.Success(transform(t.Item1, t.Item2)),
                e => Errorable.Failure<R>(e));
        }

        public static Errorable<R> Map<A, B, C, R>(
            this Errorable<(A, B, C)> errorable,
            Func<A, B, C, R> transform)
        {
            if (errorable == null)
            {
                throw new ArgumentNullException(nameof(errorable));
            }

            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            return errorable.Match(
                t => Errorable.Success(transform(t.Item1, t.Item2, t.Item3)),
                e => Errorable.Failure<R>(e));
        }
    }
}
