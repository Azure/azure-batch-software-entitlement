using System.Collections.Generic;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Factory methods for instances of <see cref="Errorable{T}"/>
    /// </summary>
    public static class Errorable
    {
        /// <summary>
        /// Create a value that represents a successful operation with a result
        /// </summary>
        /// <typeparam name="T">The type of value contained.</typeparam>
        /// <param name="value">Result value to wrap.</param>
        /// <returns>An errorable containing the provided value.</returns>
        public static Errorable<T> Success<T>(T value)
            => Errorable<T>.CreateSuccess(value);

        public static Errorable<T> Failure<T>(ErrorCollection errors)
            => Errorable<T>.CreateFailure(errors);

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="errors">Sequence of error messages.</param>
        /// <returns>An errorable containing the specified errors.</returns>
        public static Errorable<T> Failure<T>(IEnumerable<string> errors)
            => Errorable<T>.CreateFailure(errors);

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="error">Sequence of error messages.</param>
        /// <returns>An errorable containing the specified error.</returns>
        public static Errorable<T> Failure<T>(string error)
            => Errorable<T>.CreateFailure(error);
    }

    /// <summary>
    /// A container that either contains a value or a set of errors
    /// </summary>
    /// <typeparam name="T">The type of value contained in the successful case.</typeparam>
    public sealed class Errorable<T> : Result<T, ErrorCollection>
    {
        private Errorable(
            T value,
            ErrorCollection errors,
            bool hasValue) : base(value, errors, hasValue)
        {
        }

        public static Errorable<T> CreateSuccess(T value) =>
            new Errorable<T>(value, ErrorCollection.CreateEmpty(), true);

        public static Errorable<T> CreateFailure(ErrorCollection errors) =>
            new Errorable<T>(default, errors, false);

        public static Errorable<T> CreateFailure(IEnumerable<string> errors) =>
            new Errorable<T>(default, ErrorCollection.CreateWithErrors(errors), false);

        public static Errorable<T> CreateFailure(string error, params string[] errors) =>
            new Errorable<T>(default, ErrorCollection.CreateWithErrors(error, errors), false);
    }
}
