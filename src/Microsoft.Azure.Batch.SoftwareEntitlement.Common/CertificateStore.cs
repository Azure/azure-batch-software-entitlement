using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A simple abstraction over the framework supplied store classes
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// Find all available certificates
        /// </summary>
        /// <returns>Sequence of certificates (possibly empty).</returns>
        IEnumerable<X509Certificate2> FindAll();

        /// <summary>
        /// Find a certificate based on the provided thumbprint
        /// </summary>
        /// <param name="purpose">A use for which the certificate is needed (for human consumption).</param>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <returns>An <see cref="Result{X509Certificate2, ErrorCollection}"/> containing a certificate if found, or an
        /// error otherwise.</returns>
        Result<X509Certificate2, ErrorCollection> FindByThumbprint(string purpose, CertificateThumbprint thumbprint);
    }

    /// <summary>
    /// A simple abstraction over the framework supplied store classes
    /// </summary>
    public sealed class CertificateStore : ICertificateStore
    {
        // A list of StoreNames where we should look when finding certificates
        private readonly List<StoreName> _storeNames;

        // A list of places within each store we should look for certificates
        private readonly List<StoreLocation> _storeLocations;

        /// <summary>
        /// Initialize a new instance of the <see cref="CertificateStore"/> class
        /// </summary>
        public CertificateStore()
        {
            _storeNames = new List<StoreName>
            {
                StoreName.My,
                StoreName.TrustedPeople,
                StoreName.AddressBook,
                StoreName.AuthRoot,
                StoreName.TrustedPublisher,
                StoreName.CertificateAuthority,
                StoreName.Root
            };

            // Prefer user cert to machine cert
            _storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser,
                StoreLocation.LocalMachine
            };
        }

        /// <summary>
        /// Find all available certificates
        /// </summary>
        /// <returns>Sequence of certificates (possibly empty).</returns>
        public IEnumerable<X509Certificate2> FindAll()
        {
            var query =
                from name in _storeNames
                from location in _storeLocations
                from cert in FindAll(name, location)
                select cert;
            var result = query.ToList();
            return result;
        }

        /// <summary>
        /// Find all certificates by looking in the specified location
        /// </summary>
        /// <param name="storeName">Name of the store to search within.</param>
        /// <param name="storeLocation">Location within the store to check.</param>
        /// <returns>Sequence of certificates (possibly empty).</returns>
        private static IList<X509Certificate2> FindAll(StoreName storeName, StoreLocation storeLocation)
        {
            try
            {
                using (var store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var found = store.Certificates.Cast<X509Certificate2>().ToList();
                    return found;
                }
            }
            catch (Exception ex) when (IsExpectedOnLinux(ex))
            {
                // Some store locations not supported on Linux, just return null
                return new List<X509Certificate2>();
            }
        }

        /// <summary>
        /// Find a certificate based on the provided thumbprint
        /// </summary>
        /// <param name="purpose">A use for which the certificate is needed (for human consumption).</param>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <returns>An <see cref="Result{X509Certificate2,ErrorCollection}"/> containing a certificate if found, or an
        /// error otherwise.</returns>
        public Result<X509Certificate2, ErrorCollection> FindByThumbprint(string purpose, CertificateThumbprint thumbprint)
        {
            var query =
                from name in _storeNames
                from location in _storeLocations
                select FindByThumbprint(thumbprint, name, location);

            var candidates = query.Where(cert => cert != null).ToList();

            var certWithPrivateKey = candidates.Find(cert => cert.HasPrivateKey);
            if (certWithPrivateKey != null)
            {
                // We might have multiple copies of the same certificate available in different stores.
                // If so, prefer any copies that have their private key over those that do not
                // Certificates with private keys can be used to both encrypt/decrypt and to 
                // sign/verify - copies without can only be used to encrypt and verify.
                return certWithPrivateKey;
            }

            var certificate = candidates.FirstOrDefault();
            if (certificate != null)
            {
                return certificate;
            }

            return ErrorCollection.Create($"Did not find {purpose} certificate {thumbprint}");
        }

        /// <summary>
        /// Find a certificate based on the provided thumbprint by looking in the specified location
        /// </summary>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <param name="storeName">Name of the store to search within.</param>
        /// <param name="storeLocation">Location within the store to check.</param>
        /// <returns>An <see cref="X509Certificate2"/> containing a certificate if found, or null otherwise.</returns>
        private static X509Certificate2 FindByThumbprint(CertificateThumbprint thumbprint, StoreName storeName, StoreLocation storeLocation)
        {
            try
            {
                using (var store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var found = store.Certificates.Find(thumbprint);
                    return found.SingleOrDefault();
                }
            }
            catch (Exception ex) when (IsExpectedOnLinux(ex))
            {
                // Some store locations not supported on Linux, just return null
                return null;
            }
        }

        /// <summary>
        /// Test to see if a given exception is one we expect to encounter when running on Linux
        /// </summary>
        /// <param name="ex">The exception to test.</param>
        /// <returns>True if expected; false otherwise.</returns>
        private static bool IsExpectedOnLinux(Exception ex)
            => ex is PlatformNotSupportedException
               || ex is CryptographicException;
    }
}
