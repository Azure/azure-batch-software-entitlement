using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A simple abstraction over the framework supplied store classes
    /// </summary>
    public class CertificateStore
    {
        // Reference to a logger used for activity and diagnostics
        private readonly ILogger _logger;

        // A list of placenames where we should look when finding certificates
        private readonly List<StoreName> _storeNames;

        // A list of places within each store we should look for certificates
        private readonly List<StoreLocation> _storeLocations;

        /// <summary>
        /// Initialize a new instance of the <see cref="CertificateStore"/> class
        /// </summary>
        /// <param name="logger">Logger to use during use.</param>
        public CertificateStore(ILogger logger)
        {
            _logger = logger;
            _storeNames = new List<StoreName>
            {
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
            _logger.LogDebug("Searching for all certificates");
            var query
                = from name in _storeNames
                from location in _storeLocations
                from cert in FindAll(name, location)
                select cert;
              var result = query.ToList();
            _logger.LogInformation("Found {Count} certificates", result.Count);
            return result;
        }

        /// <summary>
        /// Find a certificate based on the provided thumbprint
        /// </summary>
        /// <param name="thumbprint">Thumbprint of the certificate we need.</param>
        /// <returns>Certificate, if found; null otherwise.</returns>
        public X509Certificate2 FindByThumbprint(CertificateThumbprint thumbprint)
        {
            _logger.LogDebug("Searching for certificate {Thumbprint}", thumbprint);
            var query
                = from name in _storeNames
                from location in _storeLocations
                select FindByThumbprint(thumbprint, name, location);

           var result =  query.FirstOrDefault(cert => cert != null);
            if (result == null)
            {
                _logger.LogWarning("Did not find certificate {Thumbprint}", thumbprint);
            }
            else
            {
                _logger.LogInformation("Found certificate {Thumbprint}", thumbprint);
                _logger.LogDebug("Friendly name {FriendlyName}", result.FriendlyName);
                _logger.LogDebug("Issued By {IssuedBy}", result.IssuerName);
            }

            return result;
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
                _logger.LogDebug(
                    "Found {0} certificates in certificate store {StoreName}/{StoreLocation}",
                    found.Count,
                    storeName,
                    storeLocation);
                return found.SingleOrDefault();
            }
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
                _logger.LogDebug(
                    "Found {0} certificates in certificate store {StoreName}/{StoreLocation}",
                    found.Count,
                    storeName,
                    storeLocation);
                return found;
            }
        }
    }
}
