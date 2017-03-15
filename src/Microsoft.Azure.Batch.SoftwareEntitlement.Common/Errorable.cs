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
        /// <param name="value">Result value to wrap.</param>
        public static Errorable<T> Success<T>(T value)
        {
            return new Errorable<T>.SuccessImplementation(value);
        }

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <param name="errors">Sequence of error messages.</param>
        public static Errorable<T> Failure<T>(IEnumerable<string> errors)
        {
            var errorSet = errors.Aggregate(
                ImmutableHashSet<string>.Empty,
                (s, e) => s.Add(e));
            return new Errorable<T>.FailureImplementation(errorSet);
        }

        /// <summary>
        /// Create a value that represents a failed operation
        /// </summary>
        /// <param name="error">Sequence of error messages.</param>
        public static Errorable<T> Failure<T>(string error)
        {
            var errors = ImmutableHashSet<string>.Empty.Add(error);
            return new Errorable<T>.FailureImplementation(errors);
        }

    }

    /// <summary>
    /// A container that either contains a value or a set of errors
    /// </summary>
    public abstract class Errorable<T>
    {
        /// <summary>
        /// Gets a value indicating if we have a value
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
        /// <param name="error">Error to record.</param>
        /// <returns>A new instance with the error included.</returns>
        public abstract Errorable<T> AddError(string error);

        /// <summary>
        /// Apply a transformation to our wrapped value, using another <see cref="Errorable{T}"/> 
        /// as a parameter
        /// </summary>
        /// <param name="parameter">Parameter value to use in the transformation.</param>
        /// <param name="transform">The transformation to apply.</param>
        /// <returns></returns>
        public abstract Errorable<R> Apply<X, R>(Errorable<X> parameter, Func<T, X, R> transform);

        /// <summary>
        /// An implementation of <see cref="Errorable{T}"/> that represents an actual value
        /// </summary>
        private class SuccessImplementation : Errorable<T>
        {
            public SuccessImplementation(T value)
            {
                Value = value;
            }

            public override bool HasValue => true;

            public override T Value { get; }

            public override ImmutableHashSet<string> Errors => ImmutableHashSet<string>.Empty;

            public override Errorable<T> AddError(string error)
            {
                var errors = ImmutableHashSet<string>.Empty.Add(error);
                return new FailureImplementation(errors);
            }

            public override Errorable<R> Apply<X, R>(Errorable<X> parameter, Func<T, X, R> transform)
            {
                return parameter.HasValue
                    ? Errorable.Success(transform(Value, parameter.Value))
                    : new Errorable<R>.FailureImplementation(Errors.Union(parameter.Errors));
            }
        }

        /// <summary>
        /// An implementation of <see cref="Errorable{T}"/> that represents a set of errors from a 
        /// failed operation
        /// </summary>
        private sealed class FailureImplementation : Errorable<T>
        {
            public FailureImplementation(ImmutableHashSet<string> errors)
            {
                Errors = errors;
            }

            public override bool HasValue => false;

            public override T Value => throw new InvalidOperationException($"No value of type {typeof(T).Name} available.");

            public override ImmutableHashSet<string> Errors { get; }

            public override Errorable<T> AddError(string error)
            {
                return new FailureImplementation(Errors.Add(error));
            }

            public override Errorable<R> Apply<X, R>(Errorable<X> parameter, Func<T, X, R> transform)
            {
                return new Errorable<R>.FailureImplementation(Errors.Union(parameter.Errors));
            }
        }
    }
}
