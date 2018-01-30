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
        {
            return new Errorable<T>.SuccessImplementation(value);
        }

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="errors">Sequence of error messages.</param>
        /// <returns>An errorable containing the specified errors.</returns>
        public static Errorable<T> Failure<T>(IEnumerable<string> errors)
        {
            var errorSet = ImmutableHashSet<string>.Empty.Union(errors);
            return new Errorable<T>.FailureImplementation(errorSet);
        }

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <typeparam name="T">The type of value that might have been contained.</typeparam>
        /// <param name="error">Sequence of error messages.</param>
        /// <returns>An errorable containing the specified error.</returns>
        public static Errorable<T> Failure<T>(string error)
        {
            var errors = ImmutableHashSet<string>.Empty.Add(error);
            return new Errorable<T>.FailureImplementation(errors);
        }
    }

    /// <summary>
    /// A container that either contains a value or a set of errors
    /// </summary>
    /// <typeparam name="T">The type of value contained in the successful case.</typeparam>
    public abstract class Errorable<T>
    {
        /// <summary>
        /// Gets a value indicating whether we have a value
        /// </summary>
        public abstract bool HasValue { get; }

        /// <summary>
        /// Gets the value wrapped by this <see cref="Errorable{T}"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">If no value is available.</exception>
        public abstract T Value { get; }

        /// <summary>
        /// Gets the (possibly empty) set of errors reported
        /// </summary>
        public abstract ImmutableHashSet<string> Errors { get; }

        /// <summary>
        /// Add an error
        /// </summary>
        /// <remarks>Will abandon any wrapped value if this is the first error encountered.</remarks>
        /// <param name="message">Error to record.</param>
        /// <returns>A new instance with the error included.</returns>
        public abstract Errorable<T> AddError(string message);

        /// <summary>
        /// Add a sequence of errors
        /// </summary>
        /// <remarks>Will abandon any wrapped value if these are first errors encountered.</remarks>
        /// <param name="errors">Errors to record.</param>
        /// <returns>A new instance with unique errors included.</returns>
        public Errorable<T> AddErrors(IEnumerable<string> errors)
        {
            var errorSet = errors.Aggregate(
                Errors,
                (s, e) => s.Add(e));
            return new FailureImplementation(errorSet);
        }

        /// <summary>
        /// Take an action depending on whether we have a value or some errors
        /// </summary>
        /// <param name="whenSuccessful">Action to take when we have a value.</param>
        /// <param name="whenFailure">Action to take when we have errors.</param>
        public abstract void Match(Action<T> whenSuccessful, Action<IEnumerable<string>> whenFailure);

        /// <summary>
        /// Call one function or another depending on whether we have a value or some errors
        /// </summary>
        /// <remarks>Both functions must return the same type.</remarks>
        /// <typeparam name="R">Type of value to return.</typeparam>
        /// <param name="whenSuccessful">Function to call when we have a value.</param>
        /// <param name="whenFailure">Function to call when we have errors.</param>
        /// <returns>The result of the function that was called.</returns>
        public abstract R Match<R>(Func<T, R> whenSuccessful, Func<IEnumerable<string>, R> whenFailure);

        /// <summary>
        /// Private constructor to prevent other subclasses
        /// </summary>
        private Errorable()
        {
        }

        /// <summary>
        /// An implementation of <see cref="Errorable{T}"/> that represents an actual value
        /// </summary>
        internal sealed class SuccessImplementation : Errorable<T>
        {
            public SuccessImplementation(T value)
            {
                Value = value;
            }

            public override bool HasValue => true;

            public override T Value { get; }

            public override ImmutableHashSet<string> Errors => ImmutableHashSet<string>.Empty;

            public override Errorable<T> AddError(string message)
            {
                var errors = ImmutableHashSet<string>.Empty.Add(message);
                return new FailureImplementation(errors);
            }

            /// <summary>
            /// Take an action depending on whether we have a value or some errors
            /// </summary>
            /// <param name="whenSuccessful">Action to take when we have a value.</param>
            /// <param name="whenFailure">Action to take when we have errors.</param>
            public override void Match(Action<T> whenSuccessful, Action<IEnumerable<string>> whenFailure)
            {
                whenSuccessful(Value);
            }

            /// <summary>
            /// Call one function or another depending on whether we have a value or some errors
            /// </summary>
            /// <typeparam name="R">Type of value to return.</typeparam>
            /// <param name="whenSuccessful">Function to call when we have a value.</param>
            /// <param name="whenFailure">Function to call when we have errors.</param>
            /// <returns>The result of applying the appropriate function.</returns>
            public override R Match<R>(Func<T, R> whenSuccessful, Func<IEnumerable<string>, R> whenFailure)
            {
                return whenSuccessful(Value);
            }
        }

        /// <summary>
        /// An implementation of <see cref="Errorable{T}"/> that represents a set of errors from a
        /// failed operation
        /// </summary>
        internal sealed class FailureImplementation : Errorable<T>
        {
            public FailureImplementation(ImmutableHashSet<string> errors)
            {
                Errors = errors;
            }

            public override bool HasValue => false;

            public override T Value => throw new InvalidOperationException($"No value of type {typeof(T).Name} available.");

            public override ImmutableHashSet<string> Errors { get; }

            public override Errorable<T> AddError(string message)
            {
                return new FailureImplementation(Errors.Add(message));
            }

            /// <summary>
            /// Take an action depending on whether we have a value or some errors
            /// </summary>
            /// <param name="whenSuccessful">Action to take when we have a value.</param>
            /// <param name="whenFailure">Action to take when we have errors.</param>
            public override void Match(Action<T> whenSuccessful, Action<IEnumerable<string>> whenFailure)
            {
                whenFailure(Errors);
            }

            /// <summary>
            /// Call one function or another depending on whether we have a value or some errors
            /// </summary>
            /// <typeparam name="R">Type of value to return.</typeparam>
            /// <param name="whenSuccessful">Function to call when we have a value.</param>
            /// <param name="whenFailure">Function to call when we have errors.</param>
            /// <returns>The result of applying the appropriate function.</returns>
            public override R Match<R>(Func<T, R> whenSuccessful, Func<IEnumerable<string>, R> whenFailure)
            {
                return whenFailure(Errors);
            }
        }
    }
}
