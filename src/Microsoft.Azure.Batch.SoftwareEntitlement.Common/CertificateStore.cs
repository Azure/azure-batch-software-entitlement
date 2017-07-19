using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A simple abstraction over the framework supplied store classes
    /// </summary>
    public sealed class CertificateStore
    {
        // A list of placenames where we should look when finding certificates
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
        /// <returns>Certificate, if found; null otherwise.</returns>
        private IList<X509Certificate2> FindAll(StoreName storeName, StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var found = store.Certificates.Cast<X509Certificate2>().ToList();
                return found;
            }
        }

        /// <summary>
        /// Find a certificate based on the provided thumbprint
        /// </summary>
        /// <param name="purpose">A use for which the certificate is needed (for human consumption).</param>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <returns>Certificate, if found; null otherwise.</returns>
        public Errorable<X509Certificate2> FindByThumbprint(string purpose, CertificateThumbprint thumbprint)
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
                return Errorable.Success(certWithPrivateKey);
            }

            var certificate = candidates.FirstOrDefault();
            if (certificate != null)
            {
                return Errorable.Success(certificate);
            }

            return Errorable.Failure<X509Certificate2>($"Did not find {purpose} certificate {thumbprint}");
        }

        /// <summary>
        /// Find a certificate based on the provided thumbprint by looking in the specified location
        /// </summary>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <param name="storeName">Name of the store to search within.</param>
        /// <param name="storeLocation">Location within the store to check.</param>
        /// <returns>Certificate, if found; null otherwise.</returns>
        private X509Certificate2 FindByThumbprint(CertificateThumbprint thumbprint, StoreName storeName, StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var found = store.Certificates.Find(thumbprint);
                return found.SingleOrDefault();
            }
        }
    }
}
