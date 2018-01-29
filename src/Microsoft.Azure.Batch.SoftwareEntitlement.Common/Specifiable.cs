using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Represents a value which may or may not be specified.
    /// </summary>
    /// <remarks>
    /// This is independent of whether the value is null or not;
    /// a value may be specified as null.
    /// </remarks>
    /// <typeparam name="T">The type of the value to be specified.</typeparam>
    public struct Specifiable<T> : IEquatable<Specifiable<T>>
    {
        /// <summary>
        /// Whether the value is specified.
        /// </summary>
        public bool IsSpecified { get; }

        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of a specifiable value interpreted as
        /// being specified.
        /// </summary>
        /// <remarks>
        /// To create an unspecified instance, use the default (parameterless)
        /// constructor.
        /// </remarks>
        /// <param name="value">The specified value (can be null).</param>
        public Specifiable(T value)
        {
            IsSpecified = true;
            _value = value;
        }

        /// <summary>
        /// Returns a default value if the current instance is not specified.
        /// </summary>
        /// <param name="other">The value to return if this instance is not specified</param>
        /// <returns>
        /// The specified value if this instance is specified, otherwise returns the
        /// <paramref name="other"/> value.
        /// </returns>
        public T OrDefault(T other) => IsSpecified ? _value : other;

        /// <summary>
        /// Test to see if another <see cref="Specifiable{T}"/> is equal to this instance
        /// </summary>
        /// <param name="other">Other specifiable for comparison.</param>
        /// <returns>True if both contain the same value, false otherwise.</returns>
        public bool Equals(Specifiable<T> other)
            => IsSpecified == other.IsSpecified
               && Equals(_value, other._value);

        /// <summary>
        /// Test to see if another object is equal to this instance
        /// </summary>
        /// <param name="obj">Other object for comparison.</param>
        /// <returns>True if both contain the same value, false otherwise.</returns>
        public override bool Equals(object obj)
            => obj is Specifiable<T> s && Equals(s);

        /// <summary>
        /// Generate a hash code based on our contained value
        /// </summary>
        /// <returns>Hash code based on <see cref="_value"/> or 0 if we hold a null.</returns>
        public override int GetHashCode()
            => _value?.GetHashCode() ?? 0;

        public static bool operator ==(Specifiable<T> left, Specifiable<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Specifiable<T> left, Specifiable<T> right)
        {
            return !left.Equals(right);
        }
    }
}
