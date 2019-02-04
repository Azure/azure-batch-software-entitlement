using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with <see cref="X509Certificate2"/>
    /// </summary>
    public static class X509Certificate2Extensions
    {
        private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

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

        /// <summary>
        /// Tests whether a certificate supports server authentication.
        /// </summary>
        /// <param name="certificate">Certificate to test.</param>
        /// <returns>True if the certificate supports server authentication; false otherwise.</returns>
        public static bool SupportsServerAuthentication(this X509Certificate2 certificate)
        {
            var oidValues =
                from extension in certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>()
                from oid in extension.EnhancedKeyUsages.OfType<Oid>()
                select oid.Value;

            return oidValues.Any(oid => oid.Equals(ServerAuthenticationOid, StringComparison.Ordinal));
        }
    }
}
