using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with Uris
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Check to see if the Uri has a specific scheme
        /// </summary>
        /// <param name="uri">Uri to check.</param>
        /// <param name="scheme">Scheme of interest.</param>
        /// <returns>True if the uri has the specified scheme; false otherwise.</returns>
        public static bool HasScheme(this Uri uri, string scheme)
        {
            return string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase);
        }
    }
}
