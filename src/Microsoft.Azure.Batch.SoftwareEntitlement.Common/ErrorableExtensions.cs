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
        /// <typeparam name="A">Type of value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="B">Type of value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{T}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{T}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either both values or a combined 
        /// set of error messages.</returns>
        public static Errorable<(A, B)> With<A, B>(this Errorable<A> left, Errorable<B> right)
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
                    whenSuccessful: _ => Errorable.Failure<(A, B)>(leftErrors),
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
        public static Errorable<(A, B, C)> With<A, B, C>(this Errorable<(A, B)> left, Errorable<C> right)
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
                    whenSuccessful: _ => Errorable.Failure<(A, B, C)>(leftErrors),
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
        public static Errorable<(A, B, C)> With<A, B, C>(this Errorable<A> left, Errorable<(B, C)> right)
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
                    whenSuccessful: _ => Errorable.Failure<(A, B, C)>(leftErrors),
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
        /// <typeparam name="A">Type of the first value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="B">Type of the second value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="C">Type of the third value held by <paramref name="left"/>.</typeparam>
        /// <typeparam name="D">Type of value held by <paramref name="right"/>.</typeparam>
        /// <param name="left">First <see cref="Errorable{T}"/> to combine.</param>
        /// <param name="right">Second <see cref="Errorable{T}"/> to combine.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either all three values or a 
        /// combined set of error messages.</returns>
        public static Errorable<(A, B, C, D)> With<A, B, C, D>(this Errorable<(A, B, C)> left, Errorable<D> right)
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
                    whenSuccessful: rightValue => Errorable.Success((leftValue.Item1, leftValue.Item2, leftValue.Item3, rightValue)),
                    whenFailure: errors => Errorable.Failure<(A, B, C, D)>(errors)),
                whenFailure: leftErrors => right.Match(
                    whenSuccessful: _ => Errorable.Failure<(A, B, C, D)>(leftErrors),
                    whenFailure: errors => Errorable.Failure<(A, B, C, D)>(leftErrors).AddErrors(errors)));
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

        public static void Do<A, B, C, D>(
            this Errorable<(A, B, C, D)> errorable,
            Action<A, B, C, D> whenSuccessful,
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

            errorable.Match(t => whenSuccessful(t.Item1, t.Item2, t.Item3, t.Item4), whenFailure);
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

        public static Errorable<R> Map<A, B, C, D, R>(
            this Errorable<(A, B, C, D)> errorable,
            Func<A, B, C, D, R> transform)
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
                t => Errorable.Success(transform(t.Item1, t.Item2, t.Item3, t.Item4)),
                e => Errorable.Failure<R>(e));
        }

        public static Task<Errorable<R>> MapAsync<A, B, C, D, R>(
            this Errorable<(A, B, C, D)> errorable,
            Func<A, B, C, D, Task<R>> transform)
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
                async t => Errorable.Success(await transform(t.Item1, t.Item2, t.Item3, t.Item4)),
                e => Task.FromResult(Errorable.Failure<R>(e)));
        }

        /// <summary>
        /// Configure an existing subject using a supplied value and transformation
        /// </summary>
        /// <remarks>Preserves any/all errors present on <paramref name="subject"/> and <paramref name="value"/>.</remarks>
        /// <typeparam name="S">Type of the subject to configure.</typeparam>
        /// <typeparam name="V">Type of the value to use for configuration.</typeparam>
        /// <param name="subject">Subject instance to configure, wrapped as an <see cref="Errorable{T}"/>.</param>
        /// <param name="value">Value to use for configuration, wrapped as an <see cref="Errorable{T}"/>.</param>
        /// <param name="applyConfiguration">Action to use for configuration.</param>
        /// <returns>Result of configuration.</returns>
        public static Errorable<S> Configure<S, V>(this Errorable<S> subject, Errorable<V> value, Func<S, V, S> applyConfiguration)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return subject.With(value).Map(applyConfiguration);
        }

        /// <summary>
        /// Configure an existing subject using an optionally supplied value and transformation
        /// </summary>
        /// <remarks>Preserves any/all errors present on <paramref name="subject"/> and <paramref name="value"/>.</remarks>
        /// <typeparam name="S">Type of the subject to configure.</typeparam>
        /// <typeparam name="V">Type of the value to use for configuration.</typeparam>
        /// <param name="subject">Subject instance to configure, wrapped as an <see cref="Errorable{T}"/></param>
        /// <param name="value">Value to use for configuration, wrapped as an <see cref="Errorable{T}"/>; null if not available.</param>
        /// <param name="applyConfiguration">Action to use for configuration.</param>
        /// <returns>Result of configuration.</returns>
        public static Errorable<S> ConfigureOptional<S, V>(this Errorable<S> subject, Errorable<V> value, Func<S, V, S> applyConfiguration)
            where V : class
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (value == null)
            {
                return subject;
            }

            return subject.With(value).Map((s, v) => v != null ? applyConfiguration(s, v) : s);
        }


        /// <summary>
        /// Configure an existing subject using a sequences of supplied values and transformation for each one
        /// </summary>
        /// <remarks>Preserves any/all errors present on <paramref name="subject"/> and <paramref name="values"/>.</remarks>
        /// <typeparam name="S">Type of the subject to configure.</typeparam>
        /// <typeparam name="V">Type of the value to use for configuration.</typeparam>
        /// <param name="subject">Subject instance to configure, wrapped as an <see cref="Errorable{T}"/>.</param>
        /// <param name="values">Value to use for configuration, wrapped as an <see cref="Errorable{T}"/>.</param>
        /// <param name="applyConfiguration">Action to use for configuration.</param>
        /// <returns>Result of configuration.</returns>
        public static Errorable<S> ConfigureAll<S, V>(this Errorable<S> subject, IEnumerable<Errorable<V>> values, Func<S, V, S> applyConfiguration)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return values.Aggregate(subject, (current, v) => current.With(v).Map(applyConfiguration));
        }


        /// <summary>
        /// Configure an existing subject using a bool value and transformation to apply if true
        /// </summary>
        /// <remarks>Preserves any/all errors present on <paramref name="subject"/> and <paramref name="value"/>.</remarks>
        /// <typeparam name="S">Type of the subject to configure.</typeparam>
        /// <param name="subject">Subject instance to configure, wrapped as an <see cref="Errorable{T}"/>.</param>
        /// <param name="value">Switch value to use (if true, our configuration will be activated).</param>
        /// <param name="applyConfiguration">Action to use for configuration.</param>
        /// <returns>Result of configuration.</returns>
        public static Errorable<S> ConfigureSwitch<S>(this Errorable<S> subject, Errorable<bool> value, Func<S, S> applyConfiguration)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return subject.With(value).Map((s, v) => v ? applyConfiguration(s) : s);
        }
    }
}
