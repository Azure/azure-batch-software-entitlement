using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with <see cref="X509Certificate2Collection"/>
    /// </summary>
    public static class X509Certificate2CollectionExtensions
    {
        /// <summary>
        /// Find any certificates matching the given thumbprint
        /// </summary>
        /// <param name="collection">Collection to search.</param>
        /// <param name="thumbprint">Thumbprint to find.</param>
        public static IList<X509Certificate2> Find(this X509Certificate2Collection collection, CertificateThumbprint thumbprint)
        {
            return collection.Find(X509FindType.FindByThumbprint, thumbprint.ToString(), true)
                .Cast<X509Certificate2>()
                .ToList();
        }
    }
}
