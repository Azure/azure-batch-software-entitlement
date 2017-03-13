using System;
using System.Threading;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// A strongly typed wrapper around <see cref="ServerOptions"/> that provides safe access to 
    /// values while automatically logging issues.
    /// </summary>
    /// <remarks>We use lazy instantiation to ensure only check things we need and those only once.
    /// </remarks>
    public class ServerOptionChecker
    {
        // Reference to the server options we wrap
        private readonly ServerOptions _options;

        // Logger used to report issues
        private readonly ILogger _logger;

        // Lazy access to our ServerUrl
        private readonly Lazy<Uri> _serverUrl;

        // Lazy access to our ConnectionThumbprint
        private readonly Lazy<CertificateThumbprint> _connectionThumbprint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOptionChecker"/> class
        /// </summary>
        /// <param name="options">Options provided on the command line.</param>
        /// <param name="logger">Logger used to report issues.</param>
        public ServerOptionChecker(ServerOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            _serverUrl = new Lazy<Uri>(
                FindServerUrl, LazyThreadSafetyMode.ExecutionAndPublication);
            _connectionThumbprint = new Lazy<CertificateThumbprint>(
                FindConnectionThumbprint, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets a value indicating whether we have all our mandatory configuration
        /// </summary>
        public bool IsFullyConfigured
            => ServerUrl != null && ConnectionThumbprint != null;

        /// <summary>
        /// Gets the server Uri we should listen to
        /// </summary>
        public Uri ServerUrl => _serverUrl.Value;

        /// <summary>
        /// Gets the thumbprint of a certificate to use for securing our Https connections
        /// </summary>
        public CertificateThumbprint ConnectionThumbprint => _connectionThumbprint.Value;

        /// <summary>
        /// Find and validate the uri to use for the server endpoint
        /// </summary>
        /// <returns>Validated uri for our server</returns>
        private Uri FindServerUrl()
        {
            if (string.IsNullOrWhiteSpace(_options.ServerUrl))
            {
                _logger.LogError("No server endpoint url specified.");
                return null;
            }

            try
            {
                var result = new Uri(_options.ServerUrl);
                if (!result.HasScheme("https"))
                {
                    _logger.LogError("Server end point must specify https://");
                    return null;
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError($"Invalid server endpoint url specified {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find the certificate to use for HTTPS connections
        /// </summary>
        /// <returns>Certificate, if found; null otherwise.</returns>
        private CertificateThumbprint FindConnectionThumbprint()
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionCertificateThumbprint))
            {
                _logger.LogError("No HTTPS certificate specified.");
                return null;
            }

            return new CertificateThumbprint(_options.ConnectionCertificateThumbprint);
        }


    }
}
