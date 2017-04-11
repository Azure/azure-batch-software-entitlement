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
        /// Combine two <see cref="Errorable{T}"/> values 
        /// </summary>
        /// <typeparam name="T">Type of value possibly present in <paramref name="first"/>.</typeparam>
        /// <typeparam name="A">Type of value possibly present in <paramref name="second"/>.</typeparam>
        /// <param name="first">First errorable value.</param>
        /// <param name="second">Second errorable value.</param>
        /// <param name="whenSuccessful">Action to take when both values are available.</param>
        /// <param name="whenFailure">Action to take if either (or both) errorables have errors.</param>
        public static void Combine<T, A>(
            this Errorable<T> first,
            Errorable<A> second,
            Action<T, A> whenSuccessful,
            Action<IEnumerable<string>> whenFailure)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (whenFailure == null)
            {
                throw new ArgumentNullException(nameof(whenFailure));
            }

            if (first.HasValue && second.HasValue)
            {
                whenSuccessful(first.Value, second.Value);
            }
            else
            {
                var allErrors = first.Errors.Union(second.Errors);
                whenFailure(allErrors);
            }
        }

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values and return a result.
        /// </summary>
        /// <remarks>Any errors already present are preserved.</remarks>
        /// <typeparam name="T">Type of value possibly present in <paramref name="first"/>.</typeparam>
        /// <typeparam name="A">Type of value possibly present in <paramref name="second"/>.</typeparam>
        /// <param name="first">First errorable value.</param>
        /// <param name="second">Second errorable value.</param>
        /// <param name="whenSuccessful">Function to combine both values when available.</param>
        public static Errorable<R> Combine<T, A, R>(
            this Errorable<T> first,
            Errorable<A> second,
            Func<T, A, R> whenSuccessful)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (first.HasValue && second.HasValue)
            {
                return Errorable.Success(whenSuccessful(first.Value, second.Value));
            }

            var allErrors = first.Errors.Union(second.Errors);
            return Errorable.Failure<R>(allErrors);
        }

        /// <summary>
        /// Combine three <see cref="Errorable{T}"/> values and return a result.
        /// </summary>
        /// <remarks>Any errors already present are preserved.</remarks>
        /// <typeparam name="T">Type of value possibly present in <paramref name="first"/>.</typeparam>
        /// <typeparam name="A">Type of value possibly present in <paramref name="second"/>.</typeparam>
        /// <typeparam name="B">Type of value possibly present in <paramref name="second"/>.</typeparam>
        /// <param name="first">First errorable value.</param>
        /// <param name="second">Second errorable value.</param>
        /// <param name="third">Third errorable value.</param>
        /// <param name="whenSuccessful">Function to combine all three values when available.</param>
        public static Errorable<R> Combine<T, A, B, R>(
            this Errorable<T> first,
            Errorable<A> second,
            Errorable<B> third,
            Func<T, A, B, R> whenSuccessful)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (third == null)
            {
                throw new ArgumentNullException(nameof(third));
            }

            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (first.HasValue && second.HasValue && third.HasValue)
            {
                return Errorable.Success(whenSuccessful(first.Value, second.Value, third.Value));
            }

            var allErrors = first.Errors.Union(second.Errors).Union(third.Errors);
            return Errorable.Failure<R>(allErrors);
        }
    }
}
