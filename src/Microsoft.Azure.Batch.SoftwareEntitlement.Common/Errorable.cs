using System.Collections.Generic;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Factory methods for instances of <see cref="Result{TOk,TError}"/>
    /// </summary>
    public static class Errorable
    {
        /// <summary>
        /// Create a value that represents a successful operation with a result
        /// </summary>
        /// <typeparam name="T">The type of value contained.</typeparam>
        /// <param name="value">Result value to wrap.</param>
        /// <returns>An errorable containing the provided value.</returns>
        public static Result<T, ErrorCollection> Success<T>(T value)
            => new Result<T, ErrorCollection>(value);

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="errors">Sequence of error messages.</param>
        /// <returns>An errorable containing the specified errors.</returns>
        public static Result<T, ErrorCollection> Failure<T>(IEnumerable<string> errors)
            => new Result<T, ErrorCollection>(ErrorCollection.Create(errors));

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="error">Sequence of error messages.</param>
        /// <param name="errors"></param>
        /// <returns>An errorable containing the specified error.</returns>
        public static Result<T, ErrorCollection> Failure<T>(string error, params string[] errors)
            => new Result<T, ErrorCollection>(ErrorCollection.Create(error, errors));
    }
}
