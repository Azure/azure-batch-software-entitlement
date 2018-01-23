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
    public struct Specifiable<T>
    {
        /// <summary>
        /// Whether the value is specified.
        /// </summary>
        public bool IsSpecified;

        private T _value;

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
    }
}
