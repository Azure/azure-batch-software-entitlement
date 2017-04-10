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
    }
}
