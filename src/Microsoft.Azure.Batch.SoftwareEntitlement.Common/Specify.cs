namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Static factory methods for <see cref="Specifiable{T}"/>
    /// </summary>
    public static class Specify
    {
        /// <summary>
        /// Creates a new <see cref="Specifiable{T}"/> with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the value to be specified.</typeparam>
        /// <param name="value">The value to be specified.</param>
        /// <returns>New instance.</returns>
        public static Specifiable<T> As<T>(T value) => new Specifiable<T>(value);
    }
}
