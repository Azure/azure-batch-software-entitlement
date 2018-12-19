using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
    public class Errorable<T>
    {
        /// <summary>
        /// A value indicating whether we have a value
        /// </summary>
        private readonly bool _hasValue;

        /// <summary>
        /// The value wrapped by this <see cref="Errorable{T}"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">If no value is available.</exception>
        private readonly T _value;

        /// <summary>
        /// Gets the (possibly empty) set of errors reported
        /// </summary>
        private readonly ImmutableHashSet<string> _errors;

        private Errorable(
            T value,
            ImmutableHashSet<string> errors,
            bool hasValue)
        {
            _value = value;
            _errors = errors;
            _hasValue = hasValue;
        }

        public static Errorable<T> CreateSuccess(T value)
            => new Errorable<T>(value, ImmutableHashSet.Create<string>(), true);

        public static Errorable<T> CreateFailure(IEnumerable<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            var errorHashSet = errors.ToImmutableHashSet();
            if (!errorHashSet.Any())
            {
                throw new ArgumentException("At least one error must be specified", nameof(errors));
            }

            return new Errorable<T>(default, errorHashSet, false);
        }

        public static Errorable<T> CreateFailure(string error, params string[] errors)
        {
            if (string.IsNullOrEmpty(error))
            {
                throw new ArgumentNullException(nameof(error));
            }

            var allErrors = errors != null && errors.Length > 0
                ? new[] {error}.Concat(errors).ToImmutableHashSet()
                : ImmutableHashSet.Create(error);

            return new Errorable<T>(default, allErrors, false);
        }

        /// <summary>
        /// Take an action depending on whether we have a value or some errors
        /// </summary>
        /// <param name="whenSuccessful">Action to take when we have a value.</param>
        /// <param name="whenFailure">Action to take when we have errors.</param>
        private void Match(Action<T> whenSuccessful, Action<IEnumerable<string>> whenFailure)
        {
            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (whenFailure == null)
            {
                throw new ArgumentNullException(nameof(whenFailure));
            }

            if (_hasValue)
            {
                whenSuccessful(_value);
            }
            else
            {
                whenFailure(_errors);
            }
        }

        /// <summary>
        /// Call one function or another depending on whether we have a value or some errors
        /// </summary>
        /// <remarks>Both functions must return the same type.</remarks>
        /// <typeparam name="TNext">Type of value to return.</typeparam>
        /// <param name="whenSuccessful">Function to call when we have a value.</param>
        /// <param name="whenFailure">Function to call when we have errors.</param>
        /// <returns>The result of the function that was called.</returns>
        private TNext Match<TNext>(Func<T, TNext> whenSuccessful, Func<IEnumerable<string>, TNext> whenFailure)
        {
            if (whenSuccessful == null)
            {
                throw new ArgumentNullException(nameof(whenSuccessful));
            }

            if (whenFailure == null)
            {
                throw new ArgumentNullException(nameof(whenFailure));
            }

            return _hasValue ? whenSuccessful(_value) : whenFailure(_errors);
        }

        /// <summary>
        /// Executes a function returning an <see cref="Errorable{TNext}"/> conditionally, depending
        /// on the result of this <see cref="Errorable{T}"/> instance.
        /// </summary>
        /// <typeparam name="TNext">The return type of <paramref name="operation"/></typeparam>
        /// <param name="operation">A function to execute on the value of this instance if it
        /// is successful</param>
        /// <returns>
        /// An <see cref="Errorable{TNext}"/> containing the result of executing <paramref name="operation"/>
        /// if the input was successful, or the errors from this instance otherwise.
        /// </returns>
        public Errorable<TNext> OnSuccess<TNext>(Func<T, Errorable<TNext>> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return Match(
                whenSuccessful: operation,
                whenFailure: Errorable<TNext>.CreateFailure);
        }

        /// <summary>
        /// Executes a function returning an <see cref="Errorable{TNext}"/> conditionally, depending
        /// on the result of this <see cref="Errorable{T}"/> instance.
        /// </summary>
        /// <typeparam name="TNext">The return type of <paramref name="operation"/></typeparam>
        /// <param name="operation">A function to execute on the value of this instance if it
        /// is successful</param>
        /// <returns>
        /// An <see cref="Errorable{TNext}"/> containing the result of executing <paramref name="operation"/>
        /// if the input was successful, or the errors from this instance otherwise.
        /// </returns>
        public Errorable<TNext> OnSuccess<TNext>(Func<T, TNext> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return Match(
                whenSuccessful: t => Errorable<TNext>.CreateSuccess(operation(t)),
                whenFailure: t => Errorable<TNext>.CreateFailure(_errors));
        }

        /// <summary>
        /// Take an action if we have a value
        /// </summary>
        /// <param name="action">Action to take when we have a value.</param>
        public void OnSuccess(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Match(whenSuccessful: action, whenFailure: _ => { });
        }

        /// <summary>
        /// Take an action if we are in a failure state
        /// </summary>
        /// <param name="action"></param>
        public void OnFailure(Action<IEnumerable<string>> action) =>
            Match(whenSuccessful: _ => { }, whenFailure: action);

        /// <summary>
        /// If failed, execute a function that converts the errors to the type of the success
        /// state, and returns that.
        /// If we already have a value, returns that.
        /// </summary>
        /// <param name="errorHandler"></param>
        /// <returns></returns>
        public T OnFailure(Func<IEnumerable<string>, T> errorHandler) =>
            Match(whenSuccessful: t => t, whenFailure: errorHandler);

        /// <summary>
        /// Combine two <see cref="Errorable{T}"/> values into a single value containing a value
        /// constructed from the two success states of each <see cref="Errorable{T}"/>.
        /// </summary>
        /// <remarks>
        /// Works as a logical <c>AND</c> - if both inputs are successful, the output is successful; 
        /// if either input is a failure, the output is a failure. All error messages are preserved.
        /// </remarks>
        /// <typeparam name="TOther">Type of value held by <paramref name="otherErrorable"/>.</typeparam>
        /// <typeparam name="TResult">The type resulting from combining both values.</typeparam>
        /// <param name="otherErrorable">The <see cref="Errorable{T}"/> to combine with this one.</param>
        /// <param name="resultSelector">The function used to create a result from both values.</param>
        /// <returns>An <see cref="Errorable{T}"/> containing either both values or a combined 
        /// set of error messages.</returns>
        public Errorable<TResult> With<TOther, TResult>(
            Errorable<TOther> otherErrorable,
            Func<T, TOther, TResult> resultSelector)
        {
            if (otherErrorable == null)
            {
                throw new ArgumentNullException(nameof(otherErrorable));
            }

            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            return Match(
                whenSuccessful: local => otherErrorable.Match(
                    whenSuccessful: other => Errorable.Success(resultSelector(local, other)),
                    whenFailure: Errorable<TResult>.CreateFailure),
                whenFailure: localErrors => otherErrorable.Match(
                    whenSuccessful: other => Errorable<TResult>.CreateFailure(localErrors),
                    whenFailure: otherErrors => Errorable<TResult>.CreateFailure(localErrors.Concat(otherErrors))));
        }
    }
}
