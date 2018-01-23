using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Represents an optional result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    public interface IOption<T> : IEquatable<IOption<T>>
    {
        /// <summary>
        /// Perform one of two functions based on this option
        /// </summary>
        /// <typeparam name="R">Return type</typeparam>
        /// <param name="none">Function to perform for None{T}."/></param>
        /// <param name="some">Function to perform for Some{T}.</param>
        /// <returns></returns>
        R Match<R>(Func<R> none, Func<T, R> some);

        /// <summary>
        /// Perform one of two actions based on this option
        /// </summary>
        /// <param name="none">Action to perform for None{T}."/></param>
        /// <param name="some">Action to perform for Some{T}.</param>
        void Match(Action none, Action<T> some);

        /// <summary>
        /// Act when this option is None{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        IOption<T> WhenNone(Action action);

        /// <summary>
        /// Act when this option is Some{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        IOption<T> WhenSome(Action<T> action);

        /// <summary>
        /// Return a default value if no value is available
        /// </summary>
        /// <param name="defaultValue">Default value to return</param>
        /// <returns>
        /// Wrapped value (if <see cref="Some{T}"/>) or default value (if <see cref="None{T}"/>).
        /// </returns>
        T OrDefault(T defaultValue);
    }

    /// <summary>
    /// Static factory methods for option values
    /// </summary>
    public static class Option
    {
        /// <summary>
        /// Create a Some{T} for a given value of T
        /// </summary>
        /// <typeparam name="T">Type of value to wrap.</typeparam>
        /// <param name="value">Value to wrap.</param>
        /// <returns>New instance.</returns>
        public static IOption<T> Some<T>(T value)
        {
            return new Some<T>(value);
        }

        /// <summary>
        /// Create a None{T} for a given type
        /// </summary>
        /// <typeparam name="T">Type of value we don't have.</typeparam>
        /// <returns>New instance.</returns>
        public static IOption<T> None<T>()
        {
            return new None<T>();
        }
    }

    /// <summary>
    /// Represents the presence of an optional value
    /// </summary>
    /// <typeparam name="T">Type of the value contained.</typeparam>
    public struct Some<T> : IOption<T>
    {
        /// <summary>
        /// Gets the value contained by this Some{T}
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the Some struct 
        /// </summary>
        /// <param name="value"></param>
        public Some(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Perform one of two functions based on this option
        /// </summary>
        /// <typeparam name="R">Return type</typeparam>
        /// <param name="none">Function to perform for None{T}."/></param>
        /// <param name="some">Function to perform for Some{T}.</param>
        /// <returns></returns>
        public R Match<R>(Func<R> none, Func<T, R> some)
        {
            if (none == null)
            {
                throw new ArgumentNullException(nameof(none));
            }

            if (some == null)
            {
                throw new ArgumentNullException(nameof(some));
            }

            return some(Value);
        }

        /// <summary>
        /// Perform one of two actions based on this option
        /// </summary>
        /// <param name="none">Action to perform for None{T}."/></param>
        /// <param name="some">Action to perform for Some{T}.</param>
        public void Match(Action none, Action<T> some)
        {
            if (none == null)
            {
                throw new ArgumentNullException(nameof(none));
            }

            if (some == null)
            {
                throw new ArgumentNullException(nameof(some));
            }

            some(Value);
        }

        /// <summary>
        /// Act when this option is None{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        public IOption<T> WhenNone(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return this;
        }

        /// <summary>
        /// Act when this option is Some{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        public IOption<T> WhenSome(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action(Value);
            return this;
        }

        /// <summary>
        /// Return the value we wrap
        /// </summary>
        /// <param name="defaultValue">Default value to ignore.</param>
        /// <returns>
        /// Wrapped value.
        /// </returns>
        public T OrDefault(T defaultValue)
        {
            return Value;
        }

        /// <summary>
        /// Compare this instance with another option value
        /// </summary>
        /// <param name="other">Other value to compare with</param>
        /// <returns>True if the two values are equal (value semantics).</returns>
        public bool Equals(IOption<T> other)
            => other is Some<T> s
               && Value?.Equals(s.Value) == true;

        /// <summary>
        /// Test for equality with any other object
        /// </summary>
        /// <param name="obj">Object for comparison.</param>
        /// <returns>True if the same value, false otherwise.</returns>
        public override bool Equals(object obj)
            => obj is IOption<T> opt && Equals(opt);
    }

    /// <summary>
    /// Represents the abscence of an optional value
    /// </summary>
    /// <typeparam name="T">Type of value we don't have.</typeparam>
    public sealed class None<T> : IOption<T>
    {
        /// <summary>
        /// Compare this instance with another option value
        /// </summary>
        /// <param name="other">Other value to compare with</param>
        /// <returns>True if the two values are equal (value semantics).</returns>
        public bool Equals(IOption<T> other)
            => other is None<T>;

        /// <summary>
        /// Test for equality with any other object
        /// </summary>
        /// <param name="obj">Object for comparison.</param>
        /// <returns>True if the same value, false otherwise.</returns>
        public override bool Equals(object obj)
            => obj is None<T>;


        /// <summary>
        /// Perform one of two functions based on this option
        /// </summary>
        /// <typeparam name="R">Return type</typeparam>
        /// <param name="none">Function to perform for None{T}."/></param>
        /// <param name="some">Function to perform for Some{T}.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
        public R Match<R>(Func<R> none, Func<T, R> some)
        {
            if (none == null)
            {
                throw new ArgumentNullException(nameof(none));
            }

            if (some == null)
            {
                throw new ArgumentNullException(nameof(some));
            }

            return none();
        }

        /// <summary>
        /// Perform one of two actions based on this option
        /// </summary>
        /// <param name="none">Action to perform for None{T}."/></param>
        /// <param name="some">Action to perform for Some{T}.</param>
        public void Match(Action none, Action<T> some)
        {
            if (none == null)
            {
                throw new ArgumentNullException(nameof(none));
            }

            if (some == null)
            {
                throw new ArgumentNullException(nameof(some));
            }

            none();
        }

        /// <summary>
        /// Act when this option is None{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        public IOption<T> WhenNone(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action();
            return this;
        }

        /// <summary>
        /// Return a default value
        /// </summary>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>
        /// Default value provided.
        /// </returns>
        public T OrDefault(T defaultValue)
        {
            return defaultValue;
        }

        /// <summary>
        /// Act when this option is Some{T}
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <returns>This option, for chaining.</returns>
        public IOption<T> WhenSome(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return this;
        }
    }
}
