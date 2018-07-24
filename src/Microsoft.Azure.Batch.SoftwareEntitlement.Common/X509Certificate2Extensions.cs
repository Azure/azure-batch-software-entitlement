using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with <see cref="X509Certificate2"/>
    /// </summary>
    public static class X509Certificate2Extensions
    {
        /// <summary>
        /// Test to see if a certificate supports a specific usage
        /// </summary>
        /// <param name="certificate">Certificate to test.</param>
        /// <param name="use">The usage we want to know about.</param>
        /// <returns>True if the certificate supports the specified usage; false otherwise.</returns>
        public static bool SupportsUse(this X509Certificate2 certificate, X509KeyUsageFlags use)
        {
            if (certificate?.Extensions == null)
            {
                return false;
            }

            return certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages.HasFlag(use) ?? true;
        }
    }
}
